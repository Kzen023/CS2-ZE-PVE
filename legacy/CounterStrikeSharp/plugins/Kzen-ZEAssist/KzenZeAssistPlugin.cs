using System.Text.Json;
using System.Text.RegularExpressions;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace KzenZeAssist;

public sealed class KzenZeAssistPlugin : BasePlugin
{
    private const string Prefix = "[Kzen-ZE]";
    private static readonly Regex CountdownPattern = new(@"(?<!\d)(\d{1,3})\s*(seconds?|secs?|s|秒|minutes?|mins?|m|分钟)(?!\w)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly Dictionary<int, CounterSample> _counterSamples = new();
    private readonly Dictionary<int, float> _highlightedButtons = new();
    private readonly Dictionary<string, Dictionary<int, int>> _learnedButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, HealthState> _playerHealth = new();
    private CountdownState? _countdown;
    private HealthState? _health;
    private int _lastPressedButton = -1;
    private float _lastPressedAt;
    private float _nextHudBroadcastAt;
    private float _nextCountdownHudAt;
    private string _lastHudText = string.Empty;
    private string _mapName = string.Empty;
    private int _sayText2MessageId = -1;

    public override string ModuleName => "Kzen ZE Assist";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Kzen";
    public override string ModuleDescription => "Unified ZE countdown, entity health, and learned button guidance.";

    public override void Load(bool hotReload)
    {
        AddCommandListener("say", OnSay, HookMode.Pre);
        AddCommand("css_kzen_countdown", "css_kzen_countdown <seconds> [text]", SetCountdownCommand);
        HookEntityOutput("func_button", "OnPressed", OnButtonPressed, HookMode.Post);
        HookEntityOutput("func_breakable", "OnHealthChanged", OnHealthChanged, HookMode.Post);
        HookEntityOutput("func_physbox", "OnHealthChanged", OnHealthChanged, HookMode.Post);
        HookEntityOutput("func_physbox_multiplayer", "OnHealthChanged", OnHealthChanged, HookMode.Post);
        HookEntityOutput("math_counter", "OutValue", OnCounterValue, HookMode.Post);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnEntityTakeDamagePre>(OnEntityTakeDamagePre);
        RegisterListener<Listeners.OnMetamodAllPluginsLoaded>(HookConsoleCountdownMessages);
        RegisterListener<Listeners.OnTick>(RenderCountdownHud);
    }

    private void HookConsoleCountdownMessages()
    {
        _sayText2MessageId = UserMessage.FindIdByName("SayText2");
        if (_sayText2MessageId < 0)
        {
            Server.PrintToConsole($"{Prefix} countdown debug: SayText2 message was not found.");
            return;
        }

        HookUserMessage(_sayText2MessageId, OnSayText2, HookMode.Post);
        Server.PrintToConsole($"{Prefix} countdown debug: SayText2 hook enabled (id={_sayText2MessageId}).");
    }

    private HookResult OnSayText2(UserMessage message)
    {
        var raw = message.DebugString;
        if (!CountdownPattern.IsMatch(raw))
        {
            return HookResult.Continue;
        }

        var parts = new List<string>();
        for (var index = 0; index < 8; index++)
        {
            try
            {
                var part = message.ReadString("params", index);
                if (!string.IsNullOrWhiteSpace(part))
                {
                    parts.Add(part);
                }
            }
            catch
            {
                break;
            }
        }

        var text = parts.Count > 0 ? string.Join(' ', parts) : raw;
        Server.PrintToConsole($"{Prefix} countdown debug: received SayText2: {text.Replace('\n', ' ')}");
        TryStartCountdownFromText(text);
        return HookResult.Continue;
    }

    private void OnMapStart(string mapName)
    {
        _mapName = mapName;
        _countdown = null;
        _health = null;
        _playerHealth.Clear();
        _counterSamples.Clear();
        _highlightedButtons.Clear();
        _lastPressedButton = -1;
        _nextHudBroadcastAt = 0.0f;
        _nextCountdownHudAt = 0.0f;
        _lastHudText = string.Empty;
        LoadLearnedButtons();
        AddTimer(0.25f, RenderHud, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        AddTimer(1.0f, UpdateButtonGuide, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        AddTimer(3.0f, ReportRecognizedButtons, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo command)
    {
        var message = command.ArgString.Trim().Trim('"');
        TryStartCountdownFromText(message);
        return HookResult.Continue;
    }

    private void SetCountdownCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (command.ArgCount < 2 || !int.TryParse(command.GetArg(1), out var seconds))
        {
            return;
        }

        var text = command.ArgCount > 2 ? string.Join(' ', Enumerable.Range(2, command.ArgCount - 2).Select(command.GetArg)) : "倒计时";
        StartCountdown(seconds, text);
    }

    private void TryStartCountdownFromText(string message)
    {
        var match = CountdownPattern.Match(message);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var amount))
        {
            return;
        }

        var unit = match.Groups[2].Value;
        if (unit.StartsWith('m') || unit.Contains("分钟", StringComparison.Ordinal))
        {
            amount *= 60;
        }

        if (amount is < 3 or > 900)
        {
            return;
        }

        var text = message.Remove(match.Index, match.Length).Trim(' ', '-', ':', '|', '>', '<');
        StartCountdown(amount, string.IsNullOrWhiteSpace(text) ? "目标倒计时" : text);
    }

    private void StartCountdown(int seconds, string text)
    {
        _countdown = new CountdownState(text, Server.CurrentTime + seconds);
        Server.PrintToConsole($"{Prefix} countdown debug: started {seconds}s, text={text}");
        if (_lastPressedButton >= 0 && Server.CurrentTime - _lastPressedAt <= 5.0f)
        {
            LearnButton(_lastPressedButton);
        }
    }

    private HookResult OnCounterValue(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        var counter = Utilities.GetEntityFromIndex<CMathCounter>((int)caller.Index);
        if (counter is not { IsValid: true })
        {
            return HookResult.Continue;
        }

        var now = Server.CurrentTime;
        var current = counter.Health;
        var id = (int)caller.Index;
        if (_counterSamples.TryGetValue(id, out var previous) &&
            previous.Value - current == 1 &&
            now - previous.Time is >= 0.5f and <= 1.5f &&
            current is >= 0 and <= 180)
        {
            previous.Streak++;
            if (previous.Streak >= 3)
            {
                StartCountdown(current, GetEntityLabel(caller, "目标倒计时"));
            }
        }
        else
        {
            previous = new CounterSample();
        }

        previous.Value = current;
        previous.Time = now;
        _counterSamples[id] = previous;
        return HookResult.Continue;
    }

    private HookResult OnHealthChanged(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        var entity = Utilities.GetEntityFromIndex<CBaseModelEntity>((int)caller.Index);
        if (entity is not { IsValid: true } || entity.MaxHealth <= 0 || entity.Health < 0)
        {
            return HookResult.Continue;
        }

        _health = new HealthState(GetEntityLabel(caller, $"实体 #{caller.Index}"), entity.Health, entity.MaxHealth, 0, Server.CurrentTime + 4.0f, IsBoss: entity.MaxHealth >= 500);
        return HookResult.Continue;
    }

    private HookResult OnEntityTakeDamagePre(CEntityInstance caller, CTakeDamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0 || IsPlayerPawn(caller))
        {
            return HookResult.Continue;
        }

        var attackerPawn = damageInfo.Attacker.Value?.As<CCSPlayerPawn>();
        var attacker = attackerPawn?.Controller.Value?.As<CCSPlayerController>();
        if (attacker is not { IsValid: true, IsBot: false, IsHLTV: false, Team: CsTeam.CounterTerrorist })
        {
            return HookResult.Continue;
        }

        var entity = Utilities.GetEntityFromIndex<CBaseModelEntity>((int)caller.Index);
        if (entity is not { IsValid: true } || entity.MaxHealth <= 0 || entity.Health < 0)
        {
            return HookResult.Continue;
        }

        var remainingHealth = Math.Max(0, (int)Math.Ceiling(entity.Health - damageInfo.Damage));
        var health = new HealthState(
            GetEntityLabel(caller, GetFriendlyEntityName(caller.DesignerName, caller.Index)),
            remainingHealth,
            entity.MaxHealth,
            Math.Max(1, (int)Math.Round(damageInfo.Damage)),
            Server.CurrentTime + 4.0f,
            IsBoss: entity.MaxHealth >= 500);
        _playerHealth[attacker.Slot] = health;
        ShowHealthHud(attacker, health);
        return HookResult.Continue;
    }

    private static bool IsPlayerPawn(CEntityInstance entity)
    {
        return entity.DesignerName.Contains("player", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFriendlyEntityName(string designerName, uint index)
    {
        return designerName switch
        {
            "func_breakable" => "可破坏物",
            "func_breakable_surf" => "玻璃",
            "func_physbox" or "func_physbox_multiplayer" => "木板/物件",
            _ => $"实体 #{index}"
        };
    }

    [GameEventHandler]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var attacker = @event.Attacker;
        if (attacker is not { IsValid: true, IsBot: false, IsHLTV: false, Team: CsTeam.CounterTerrorist } ||
            victim is not { IsValid: true, Team: CsTeam.Terrorist })
        {
            return HookResult.Continue;
        }

        var pawn = victim.PlayerPawn.Value;
        if (pawn is { IsValid: true, LifeState: (byte)LifeState_t.LIFE_ALIVE } && pawn.MaxHealth > 0)
        {
            var health = new HealthState(
                $"僵尸 {victim.PlayerName}",
                pawn.Health,
                pawn.MaxHealth,
                Math.Max(1, @event.DmgHealth),
                Server.CurrentTime + 4.0f,
                IsBoss: false);
            _playerHealth[attacker.Slot] = health;
            ShowHealthHud(attacker, health);
        }

        return HookResult.Continue;
    }

    private HookResult OnButtonPressed(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        _lastPressedButton = (int)caller.Index;
        _lastPressedAt = Server.CurrentTime;
        DisableGlow(_lastPressedButton);
        return HookResult.Continue;
    }

    private void RenderHud()
    {
        // The countdown owns the center HUD while active. Rendering health there at
        // the same time restarts the client animation and makes both unreadable.
        if (_countdown is not null)
        {
            return;
        }

        var now = Server.CurrentTime;
        if (_health is { } mapHealth && mapHealth.ExpiresAt <= now)
        {
            _health = null;
        }

        if (Server.CurrentTime < _nextHudBroadcastAt)
        {
            return;
        }

        _nextHudBroadcastAt = Server.CurrentTime + 0.5f;
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is { IsValid: true, IsBot: false, IsHLTV: false })
            {
                if (_playerHealth.TryGetValue(player.Slot, out var playerHealth) && playerHealth.ExpiresAt <= now)
                {
                    _playerHealth.Remove(player.Slot);
                    playerHealth = null!;
                }

                var health = playerHealth ?? _health;
                if (health is null)
                {
                    continue;
                }

                ShowHealthHud(player, health);
            }
        }
    }

    private void ShowHealthHud(CCSPlayerController player, HealthState health)
    {
        if (_countdown is not null || player is not { IsValid: true, IsBot: false, IsHLTV: false } || health.Maximum <= 0)
        {
            return;
        }

        var percent = Math.Clamp((int)Math.Round(health.Current * 100.0 / health.Maximum), 0, 100);
        var filled = Math.Clamp((int)Math.Round(percent / 10.0), 0, 10);
        var bar = new string('#', filled) + new string('-', 10 - filled);
        var damage = health.Damage > 0 ? $"  -{health.Damage:N0}" : string.Empty;
        _lastHudText = $"[ KZEN 目标血量 ]\n{health.Name}\nHP {health.Current:N0} / {health.Maximum:N0}  [{bar}] {percent}%{damage}";
        player.PrintToCenter(_lastHudText);
    }

    private void RenderCountdownHud()
    {
        if (_countdown is not { } countdown)
        {
            return;
        }

        if (Server.CurrentTime < _nextCountdownHudAt)
        {
            return;
        }

        // 10 Hz keeps the client-side HTML panel alive without generating a
        // network message for every server frame.
        _nextCountdownHudAt = Server.CurrentTime + 0.1f;

        var remaining = (int)Math.Ceiling(countdown.EndTime - Server.CurrentTime);
        if (remaining < 0)
        {
            _countdown = null;
            Server.PrintToConsole($"{Prefix} countdown debug: finished.");
            return;
        }

        var html = $"<font color='gray'>----</font> <font class='fontSize-l' color='gold'>KZEN 倒计时</font> <font color='gray'>----</font><br><font color='gray'>►</font> <font class='fontSize-m' color='white'>[{EscapeHtml(countdown.Text)}]</font> <font color='gray'>◄</font><br><font color='gray'>►</font> <font class='fontSize-l' color='gold'>[{remaining} 秒]</font> <font color='gray'>◄</font>";
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is { IsValid: true, IsBot: false, IsHLTV: false })
            {
                player.PrintToCenterHtml(html);
            }
        }
    }

    private void ReportRecognizedButtons()
    {
        var buttons = Utilities.FindAllEntitiesByDesignerName<CBaseButton>("func_button")
            .Where(button => button is { IsValid: true, AbsOrigin: not null })
            .OrderBy(button => button.Index)
            .ToArray();
        Server.PrintToConsole($"{Prefix} {_mapName}: recognized {buttons.Length} func_button entities.");
        foreach (var button in buttons.Take(24))
        {
            var origin = button.AbsOrigin!;
            var name = string.IsNullOrWhiteSpace(button.Entity?.Name) ? "<unnamed>" : button.Entity.Name;
            Server.PrintToConsole($"{Prefix} button #{button.Index}: {name} @ {origin.X:0},{origin.Y:0},{origin.Z:0}");
        }
    }

    private void UpdateButtonGuide()
    {
        foreach (var entry in _highlightedButtons.Where(pair => pair.Value <= Server.CurrentTime).ToArray())
        {
            DisableGlow(entry.Key);
        }

        var humans = Utilities.GetPlayers()
            .Where(player => player is { IsValid: true, IsBot: false, Team: CsTeam.CounterTerrorist })
            .Select(player => player.PlayerPawn.Value)
            .Where(pawn => pawn is { IsValid: true, AbsOrigin: not null, LifeState: (byte)LifeState_t.LIFE_ALIVE })
            .ToArray();
        if (humans.Length == 0)
        {
            return;
        }

        var learned = _learnedButtons.TryGetValue(_mapName, out var mapButtons) ? mapButtons : new Dictionary<int, int>();
        var candidates = Utilities.FindAllEntitiesByDesignerName<CBaseButton>("func_button")
            .Where(button => button is { IsValid: true, AbsOrigin: not null })
            .Select(button => new { Button = button, Score = learned.GetValueOrDefault((int)button.Index), Distance = NearestHumanDistance(button.AbsOrigin!, humans!) })
            .Where(item => item.Distance <= 3500.0f)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Distance)
            .Take(6);

        foreach (var candidate in candidates)
        {
            EnableGlow(candidate.Button);
        }
    }

    private void EnableGlow(CBaseButton button)
    {
        var glow = button.Glow;
        glow.Glowing = true;
        glow.GlowType = 3;
        glow.GlowTeam = 0;
        glow.GlowRange = 10000;
        glow.GlowRangeMin = 0;
        glow.EligibleForScreenHighlight = true;
        glow.Flashing = false;
        glow.GlowColorOverride = Color.FromArgb(255, 64, 255, 96);
        Utilities.SetStateChanged(button, "CBaseModelEntity", "m_Glow");
        _highlightedButtons[(int)button.Index] = Server.CurrentTime + 2.0f;
    }

    private void DisableGlow(int index)
    {
        _highlightedButtons.Remove(index);
        var button = Utilities.GetEntityFromIndex<CBaseButton>(index);
        if (button is { IsValid: true })
        {
            button.Glow.Glowing = false;
            Utilities.SetStateChanged(button, "CBaseModelEntity", "m_Glow");
        }
    }

    private void LearnButton(int index)
    {
        if (string.IsNullOrWhiteSpace(_mapName))
        {
            return;
        }

        if (!_learnedButtons.TryGetValue(_mapName, out var buttons))
        {
            buttons = new Dictionary<int, int>();
            _learnedButtons[_mapName] = buttons;
        }

        buttons[index] = buttons.GetValueOrDefault(index) + 1;
        SaveLearnedButtons();
        Server.PrintToConsole($"{Prefix} learned progress button #{index} on {_mapName}.");
    }

    private static float NearestHumanDistance(Vector origin, IEnumerable<CCSPlayerPawn> humans)
    {
        var nearest = float.MaxValue;
        foreach (var human in humans)
        {
            if (human.AbsOrigin is { } humanOrigin)
            {
                var x = origin.X - humanOrigin.X;
                var y = origin.Y - humanOrigin.Y;
                var z = origin.Z - humanOrigin.Z;
                nearest = Math.Min(nearest, MathF.Sqrt(x * x + y * y + z * z));
            }
        }

        return nearest;
    }

    private static string GetEntityLabel(CEntityInstance entity, string fallback)
    {
        var name = entity.Entity?.Name;
        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }

    private string LearnPath => Path.Combine(AppContext.BaseDirectory, "..", "..", "csgo", "addons", "counterstrikesharp", "configs", "plugins", "Kzen-ZEAssist", "button-learning.json");

    private void LoadLearnedButtons()
    {
        try
        {
            _learnedButtons.Clear();
            if (File.Exists(LearnPath))
            {
                foreach (var entry in JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, int>>>(File.ReadAllText(LearnPath)) ?? [])
                {
                    _learnedButtons[entry.Key] = entry.Value;
                }
            }
        }
        catch (Exception exception)
        {
            Server.PrintToConsole($"{Prefix} could not load learned buttons: {exception.Message}");
        }
    }

    private void SaveLearnedButtons()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LearnPath)!);
            File.WriteAllText(LearnPath, JsonSerializer.Serialize(_learnedButtons, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception exception)
        {
            Server.PrintToConsole($"{Prefix} could not save learned buttons: {exception.Message}");
        }
    }

    private sealed record CountdownState(string Text, float EndTime);
    private static string EscapeHtml(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private sealed record HealthState(string Name, int Current, int Maximum, int Damage, float ExpiresAt, bool IsBoss);
    private sealed class CounterSample { public int Value; public float Time; public int Streak; }
}

