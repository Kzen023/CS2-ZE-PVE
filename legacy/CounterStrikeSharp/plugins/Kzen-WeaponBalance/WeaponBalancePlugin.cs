using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;

namespace KzenWeaponBalance;

public sealed class WeaponBalancePlugin : BasePlugin
{
    public override string ModuleName => "Kzen Weapon Balance";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Kzen";
    public override string ModuleDescription => "PvE weapon magazines, purchases, and zombie-only damage balance.";

    private const string Prefix = "[Kzen-Weapons]";
    private WeaponBalanceConfig _config = new();
    private readonly HashSet<string> _nativePricesApplied = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _buyMappings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, PendingNativeBuy> _pendingNativeBuys = new();

    private static readonly Dictionary<string, string> FixesAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sy"] = "deagle", ["a1"] = "m4a1", ["sq"] = "elite",
        ["deagle"] = "deagle", ["dualberettas"] = "elite", ["elite"] = "elite", ["fiveseven"] = "fiveseven",
        ["glock18"] = "glock", ["glock"] = "glock", ["ak47"] = "ak47", ["ak"] = "ak47", ["aug"] = "aug",
        ["awp"] = "awp", ["famas"] = "famas", ["g3sg1"] = "g3sg1", ["galilar"] = "galilar", ["galil"] = "galilar",
        ["m4a4"] = "m4a1", ["mac10"] = "mac10", ["p90"] = "p90", ["mp5sd"] = "mp5sd", ["mp5"] = "mp5sd",
        ["ump45"] = "ump45", ["ump"] = "ump45", ["xm1014"] = "xm1014", ["xm"] = "xm1014", ["bizon"] = "bizon",
        ["mag7"] = "mag7", ["mag"] = "mag7", ["negev"] = "negev", ["sawedoff"] = "sawedoff", ["tec9"] = "tec9",
        ["p2000"] = "hkp2000", ["mp7"] = "mp7", ["mp9"] = "mp9", ["nova"] = "nova", ["p250"] = "p250",
        ["scar20"] = "scar20", ["scar"] = "scar20", ["sg553"] = "sg556", ["ssg08"] = "ssg08", ["ssg"] = "ssg08",
        ["m4a1-s"] = "m4a1_silencer", ["m4a1"] = "m4a1_silencer", ["usp-s"] = "usp_silencer", ["usp"] = "usp_silencer",
        ["cz75-auto"] = "cz75a", ["cs75a"] = "cz75a", ["cz"] = "cz75a", ["r8revolver"] = "revolver",
        ["revolver"] = "revolver", ["r8"] = "revolver"
    };

    private static readonly HashSet<string> BareChatAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "sy", "a1", "sq"
    };

    private static readonly HashSet<string> PrimaryWeapons = new(StringComparer.OrdinalIgnoreCase)
    {
        "mac10", "mp9", "mp5sd", "ump45", "mp7", "bizon", "p90", "nova", "mag7", "sawedoff", "xm1014",
        "galilar", "famas", "m4a1_silencer", "m4a1", "ak47", "aug", "sg556", "negev", "m249", "ssg08",
        "g3sg1", "scar20", "awp"
    };

    private static readonly HashSet<string> SecondaryWeapons = new(StringComparer.OrdinalIgnoreCase)
    {
        "glock", "usp_silencer", "hkp2000", "p250", "elite", "fiveseven", "tec9", "cz75a", "deagle", "revolver"
    };

    private static readonly Dictionary<string, int> VanillaPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["glock"] = 200, ["usp_silencer"] = 200, ["hkp2000"] = 200, ["p250"] = 300,
        ["elite"] = 300, ["fiveseven"] = 500, ["tec9"] = 500, ["cz75a"] = 500,
        ["deagle"] = 700, ["revolver"] = 600, ["mac10"] = 1050, ["mp9"] = 1250,
        ["mp5sd"] = 1500, ["ump45"] = 1200, ["mp7"] = 1500, ["bizon"] = 1400,
        ["p90"] = 2350, ["nova"] = 1050, ["mag7"] = 1300, ["sawedoff"] = 1100,
        ["xm1014"] = 2000, ["galilar"] = 1800, ["famas"] = 2050,
        ["m4a1_silencer"] = 2900, ["m4a1"] = 3100, ["ak47"] = 2700,
        ["aug"] = 3300, ["sg556"] = 3000, ["negev"] = 1700, ["m249"] = 5200,
        ["ssg08"] = 1700, ["g3sg1"] = 5000, ["scar20"] = 5000, ["awp"] = 4750
    };

    public override void Load(bool hotReload)
    {
        _config = WeaponBalanceConfig.Load();
        LoadBuyMappings();
        RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
        RegisterListener<Listeners.OnPlayerTakeDamagePre>(OnPlayerTakeDamagePre);
        RegisterEventHandler<EventItemPurchase>(OnItemPurchase);
        AddCommand("css_weapon", "购买 Kzen PvE 武器：!weapon <武器名>", OnBuyCommand);
        AddCommand("css_kbuy", "兼容命令：!kbuy <武器名>", OnBuyCommand);
        AddCommandListener("buy", OnNativeBuy, HookMode.Pre);
        AddCommandListener("say", OnSay, HookMode.Pre);

        // Kzen owns the same chat aliases so their price check happens before a
        // weapon is given. CS2Fixes ZR and all non-weapon features stay enabled.
        Server.ExecuteCommand("cs2f_weapons_enable 0");

        if (_config.InfiniteReserveAmmo)
        {
            Server.ExecuteCommand("cs2f_infinite_reserve_ammo 1");
        }

        Server.PrintToConsole($"{Prefix} Loaded {_config.Weapons.Count} weapon balance entries.");
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event.Userid?.PlayerPawn.Value is { IsValid: true } pawn)
        {
            AddTimer(0.1f, () => ApplyPawnWeapons(pawn), TimerFlags.STOP_ON_MAPCHANGE);
        }

        return HookResult.Continue;
    }

    private void OnEntityCreated(CEntityInstance entity)
    {
        if (!entity.DesignerName.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var entityIndex = entity.Index;
        Server.NextFrame(() =>
        {
            var weapon = Utilities.GetEntityFromIndex<CCSWeaponBase>((int)entityIndex);
            if (weapon is { IsValid: true })
            {
                ApplyWeapon(weapon);
            }
        });
    }

    private HookResult OnPlayerTakeDamagePre(CCSPlayerPawn victimPawn, CTakeDamageInfo damageInfo)
    {
        if (!_config.ZombieOnlyDamage || victimPawn.TeamNum != (byte)CsTeam.Terrorist)
        {
            return HookResult.Continue;
        }

        var attackerPawn = damageInfo.Attacker.Value?.As<CCSPlayerPawn>();
        var attacker = attackerPawn?.Controller.Value?.As<CCSPlayerController>();
        if (attacker is not { IsValid: true, IsBot: false, IsHLTV: false, Team: CsTeam.CounterTerrorist })
        {
            return HookResult.Continue;
        }

        var weapon = attackerPawn!.WeaponServices?.ActiveWeapon.Value;
        if (weapon is not { IsValid: true } || !TryGetWeaponBalance(weapon.DesignerName, out var balance) || balance.DamageScale == 1.0f)
        {
            return HookResult.Continue;
        }

        damageInfo.Damage *= balance.DamageScale;
        damageInfo.TotalledDamage = damageInfo.Damage;
        return HookResult.Continue;
    }

    private void OnBuyCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is not { IsValid: true, IsBot: false, IsHLTV: false, Team: CsTeam.CounterTerrorist } || command.ArgCount < 2)
        {
            player?.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}用法：!weapon <武器名>，例如 !weapon ak47");
            return;
        }

        PurchaseWeapon(player, NormalizeWeaponName(command.GetArg(1)));
    }

    private HookResult OnNativeBuy(CCSPlayerController? player, CommandInfo command)
    {
        var rawArguments = command.ArgString;
        var playerName = player is { IsValid: true } ? player.PlayerName : "<no-player>";
        var playerSlot = player is { IsValid: true } ? player.Slot : -1;
        Server.PrintToConsole($"{Prefix} DEBUG buy received: player='{playerName}' slot={playerSlot} argc={command.ArgCount} args='{rawArguments}'.");

        if (player is not { IsValid: true, IsBot: false, IsHLTV: false, Team: CsTeam.CounterTerrorist } || command.ArgCount < 2)
        {
            Server.PrintToConsole($"{Prefix} DEBUG buy passed through: invalid/non-CT player or missing weapon argument.");
            return HookResult.Continue;
        }

        var requestedName = NormalizeWeaponName(command.GetArg(1));
        if (requestedName.Equals("unused", StringComparison.OrdinalIgnoreCase))
        {
            var signature = NormalizeBuySignature(rawArguments);
            if (TryGetUnusedIndex(signature, out var unusedIndex) && unusedIndex >= 20)
            {
                Server.PrintToConsole($"{Prefix} DEBUG buy passed through: native utility/equipment signature='{signature}'.");
                return HookResult.Continue;
            }

            Server.PrintToConsole($"{Prefix} DEBUG buy blocked before native purchase: player-loadout weapon signature='{signature}'.");
            player.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}原生 B 菜单枪械栏已禁用，请用 !ak、!r8、!awp 或 !weapon <武器名> 购买。");
            return HookResult.Handled;
        }

        if (!_config.Weapons.TryGetValue(requestedName, out var balance) || !balance.Enabled)
        {
            Server.PrintToConsole($"{Prefix} DEBUG buy passed through: '{requestedName}' has no enabled Kzen price entry.");
            return HookResult.Continue;
        }

        Server.PrintToConsole($"{Prefix} DEBUG buy redirected to Kzen purchase: weapon='{requestedName}'.");
        PurchaseWeapon(player, requestedName);
        return HookResult.Handled;
    }

    public HookResult OnItemPurchase(EventItemPurchase @event, GameEventInfo info)
    {
        var player = @event.Userid;
        var weaponName = NormalizeWeaponName(@event.Weapon);
        Server.PrintToConsole($"{Prefix} DEBUG item_purchase: player='{player?.PlayerName ?? "<no-player>"}' weaponRaw='{@event.Weapon}' weapon='{weaponName}'.");
        PendingNativeBuy? pending = null;
        if (player is { IsValid: true })
            _pendingNativeBuys.Remove(player.Slot, out pending);

        if (player is not { IsValid: true, IsBot: false, IsHLTV: false, Team: CsTeam.CounterTerrorist }
            || !_config.Weapons.TryGetValue(weaponName, out var balance)
            || !balance.Enabled
            || !VanillaPrices.TryGetValue(weaponName, out var vanillaPrice))
        {
            Server.PrintToConsole($"{Prefix} DEBUG item_purchase ignored: player/team/config/vanilla-price check failed for '{weaponName}'.");
            return HookResult.Continue;
        }

        var slot = player.Slot;
        Server.PrintToConsole($"{Prefix} DEBUG item_purchase scheduled settlement: slot={slot} weapon='{weaponName}' vanilla=${vanillaPrice} kzen=${balance.Price} before=${pending?.MoneyBefore.ToString(CultureInfo.InvariantCulture) ?? "<estimated>"}.");
        AddTimer(0.02f, () => FinalizeNativePurchase(slot, weaponName, balance.Price, vanillaPrice, pending), TimerFlags.STOP_ON_MAPCHANGE);
        return HookResult.Continue;
    }

    private void FinalizeNativePurchase(int slot, string weaponName, int kzenPrice, int vanillaPrice, PendingNativeBuy? pending)
    {
        var player = Utilities.GetPlayers().FirstOrDefault(p => p.Slot == slot && p is { IsValid: true, IsBot: false, IsHLTV: false });
        if (player is null)
        {
            Server.PrintToConsole($"{Prefix} DEBUG settlement aborted: player slot={slot} is no longer valid.");
            return;
        }

        var moneyServices = player.InGameMoneyServices;
        if (moneyServices is null)
        {
            Server.PrintToConsole($"{Prefix} DEBUG settlement aborted: no money services for slot={slot}.");
            return;
        }

        var balanceBeforePurchase = pending?.MoneyBefore ?? moneyServices.Account + vanillaPrice;
        Server.PrintToConsole($"{Prefix} DEBUG settlement: player='{player.PlayerName}' weapon='{weaponName}' accountNow=${moneyServices.Account} estimatedBefore=${balanceBeforePurchase} vanilla=${vanillaPrice} kzen=${kzenPrice}.");
        if (balanceBeforePurchase < kzenPrice)
        {
            moneyServices.Account = balanceBeforePurchase;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            Server.PrintToConsole($"{Prefix} DEBUG settlement insufficient after native purchase: weapon='{weaponName}' refundedAccount=${balanceBeforePurchase}.");
            player.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}{weaponName} 价格 ${kzenPrice}，余额不足，已退款。");
            return;
        }

        moneyServices.Account = balanceBeforePurchase - kzenPrice;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        Server.PrintToConsole($"{Prefix} DEBUG settlement complete: weapon='{weaponName}' finalAccount=${moneyServices.Account} charged=${kzenPrice}.");
        player.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}已购买 {weaponName}，实际花费 ${kzenPrice}。");
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo command)
    {
        if (player is not { IsValid: true, IsBot: false, IsHLTV: false, Team: CsTeam.CounterTerrorist })
        {
            return HookResult.Continue;
        }

        var message = command.ArgString.Trim().Trim('"');
        var hasPrefix = message.StartsWith('!') || message.StartsWith('/');
        string alias;
        if (hasPrefix)
        {
            var words = message[1..].Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return HookResult.Continue;
            }

            alias = words[0];
        }
        else
        {
            if (!BareChatAliases.Contains(message))
            {
                return HookResult.Continue;
            }

            alias = message;
        }

        if (!FixesAliases.TryGetValue(alias, out var weaponName))
        {
            return HookResult.Continue;
        }

        Server.PrintToConsole($"{Prefix} Chat alias received: '{message}' -> {weaponName}.");
        PurchaseWeapon(player, weaponName);
        return HookResult.Handled;
    }

    private void PurchaseWeapon(CCSPlayerController player, string requestedName)
    {
        if (!_config.Weapons.TryGetValue(requestedName, out var balance) || !balance.Enabled)
        {
            player.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}该武器未启用或不存在。");
            return;
        }

        var moneyServices = player.InGameMoneyServices;
        var pawn = player.PlayerPawn.Value;
        var itemServices = pawn?.ItemServices?.As<CCSPlayer_ItemServices>();
        var weaponServices = pawn?.WeaponServices;
        var requestedSlot = GetWeaponSlot(requestedName);
        if (moneyServices is null || pawn is not { IsValid: true } || itemServices is null || weaponServices is null)
        {
            player.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}当前无法购买武器，请稍后重试。");
            return;
        }

        var money = moneyServices.Account;
        if (money < balance.Price)
        {
            player.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}余额不足：{requestedName} 实际价格 ${balance.Price}。");
            return;
        }

        var replacedWeapons = new List<string>();
        if (requestedSlot != 0)
        {
            foreach (var handle in weaponServices.MyWeapons.ToArray())
            {
                if (handle.Value is not { IsValid: true } ownedWeapon)
                {
                    continue;
                }

                var ownedName = NormalizeWeaponName(ownedWeapon.DesignerName);
                if (GetWeaponSlot(ownedName) != requestedSlot)
                {
                    continue;
                }

                itemServices.DropActivePlayerWeapon(ownedWeapon);
                replacedWeapons.Add(ownedName);
            }
        }

        var newWeapon = player.GiveNamedItem<CCSWeaponBase>($"weapon_{requestedName}");
        if (newWeapon is not { IsValid: true })
        {
            foreach (var replacedName in replacedWeapons)
            {
                player.GiveNamedItem($"weapon_{replacedName}");
            }

            player.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}发放武器失败，未扣除余额。");
            return;
        }

        moneyServices.Account = money - balance.Price;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        AddTimer(0.1f, () =>
        {
            if (player is { IsValid: true } && player.PlayerPawn.Value is { IsValid: true })
            {
                player.ExecuteClientCommand($"use weapon_{requestedName}");
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);
        player.PrintToChat($"{ChatColors.Green}[Kzen] {ChatColors.Default}已购买 {requestedName}，实际花费 ${balance.Price}。");
    }

    private static int GetWeaponSlot(string weaponName)
    {
        if (PrimaryWeapons.Contains(weaponName))
        {
            return 1;
        }

        return SecondaryWeapons.Contains(weaponName) ? 2 : 0;
    }

    private void ApplyPawnWeapons(CCSPlayerPawn pawn)
    {
        if (pawn is not { IsValid: true } || pawn.WeaponServices is not { } weaponServices)
        {
            return;
        }

        foreach (var handle in weaponServices.MyWeapons)
        {
            if (handle.Value is CCSWeaponBase weapon && weapon.IsValid)
            {
                ApplyWeapon(weapon);
            }
        }
    }

    private void ApplyWeapon(CCSWeaponBase weapon)
    {
        // Grenades use the same reserve-ammo field as firearms. Refilling it here
        // turns one B-menu purchase into the configured grenade carry limit.
        if (_config.InfiniteReserveAmmo && !IsThrowable(weapon.DesignerName))
        {
            weapon.AcceptInput("SetReserveAmmoAmount", null, null, "999");
        }

        if (!TryGetWeaponBalance(weapon.DesignerName, out var balance) || !balance.Enabled || balance.Clip <= 0)
        {
            return;
        }

        CCSWeaponBaseVData? vdata;
        try
        {
            vdata = weapon.VData;
        }
        catch (ArgumentNullException)
        {
            Server.PrintToConsole($"{Prefix} DEBUG skipped weapon with null VData: weapon='{weapon.DesignerName}'.");
            return;
        }

        if (vdata is null)
        {
            return;
        }

        if (balance.Price > 0)
        {
            vdata.Price = balance.Price;
            _nativePricesApplied.Add(NormalizeWeaponName(weapon.DesignerName));
            Server.PrintToConsole($"{Prefix} DEBUG native B-menu price applied: weapon='{weapon.DesignerName}' price=${balance.Price}.");
        }

        vdata.MaxClip1 = balance.Clip;
        vdata.DefaultClip1 = balance.Clip;
        if (weapon.Clip1 > balance.Clip || weapon.Clip1 <= 0)
        {
            weapon.Clip1 = balance.Clip;
        }
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
    }

    private bool TryGetWeaponBalance(string designerName, out WeaponBalance balance)
    {
        return _config.Weapons.TryGetValue(NormalizeWeaponName(designerName), out balance!);
    }

    private static string NormalizeWeaponName(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized.StartsWith("weapon_", StringComparison.Ordinal) ? normalized[7..] : normalized;
    }

    private static bool IsThrowable(string designerName)
    {
        return NormalizeWeaponName(designerName) is "hegrenade" or "flashbang" or "smokegrenade"
            or "decoy" or "molotov" or "incgrenade" or "tagrenade";
    }

    private static string NormalizeBuySignature(string value)
    {
        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim().ToLowerInvariant();
    }

    private static bool TryGetUnusedIndex(string signature, out int index)
    {
        index = 0;
        var parts = signature.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
            && parts[0].Equals("unused", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    private int GetCheapestEnabledWeaponPrice()
    {
        return _config.Weapons.Values
            .Where(weapon => weapon.Enabled && weapon.Price > 0)
            .Select(weapon => weapon.Price)
            .DefaultIfEmpty(0)
            .Min();
    }

    private static string GetBuyMappingKey(CCSPlayerController player, string signature)
    {
        var loadoutHash = player.InventoryServices?.CurrentLoadoutHash ?? 0;
        return $"{loadoutHash:X16}|{signature}";
    }

    private static string GetBuyMappingsPath()
    {
        return Path.Combine(WeaponBalanceConfig.GetConfigDirectory(), "buy_mappings.cfg");
    }

    private void LoadBuyMappings()
    {
        var path = GetBuyMappingsPath();
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            var separator = line.IndexOf('=');
            if (separator <= 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            _buyMappings[line[..separator].Trim()] = NormalizeWeaponName(line[(separator + 1)..]);
        }
    }

    private void SaveBuyMappings()
    {
        var path = GetBuyMappingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, _buyMappings.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
    }
}

internal sealed class WeaponBalanceConfig
{
    public bool InfiniteReserveAmmo { get; private set; } = true;
    public bool ZombieOnlyDamage { get; private set; } = true;
    public Dictionary<string, WeaponBalance> Weapons { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static WeaponBalanceConfig Load()
    {
        var config = new WeaponBalanceConfig();
        var directory = GetConfigDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "weapons.cfg");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, DefaultText());
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (key.Equals("infinite_reserve_ammo", StringComparison.OrdinalIgnoreCase))
            {
                config.InfiniteReserveAmmo = ParseBool(value, config.InfiniteReserveAmmo);
                continue;
            }

            if (key.Equals("zombie_only_damage", StringComparison.OrdinalIgnoreCase))
            {
                config.ZombieOnlyDamage = ParseBool(value, config.ZombieOnlyDamage);
                continue;
            }

            var parts = key.Split('.', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var weaponName = Normalize(parts[0]);
            if (!config.Weapons.TryGetValue(weaponName, out var weapon))
            {
                weapon = new WeaponBalance();
                config.Weapons[weaponName] = weapon;
            }

            weapon.Apply(parts[1], value);
        }

        return config;
    }

    private static bool ParseBool(string value, bool fallback) => value is "1" or "true" ? true : value is "0" or "false" ? false : fallback;
    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace("weapon_", string.Empty, StringComparison.Ordinal);

    internal static string GetConfigDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var cssharpIndex = baseDirectory.IndexOf("addons\\counterstrikesharp", StringComparison.OrdinalIgnoreCase);
        if (cssharpIndex < 0)
        {
            cssharpIndex = baseDirectory.IndexOf("addons/counterstrikesharp", StringComparison.OrdinalIgnoreCase);
        }

        var cssharpDirectory = cssharpIndex >= 0
            ? baseDirectory[..(cssharpIndex + "addons\\counterstrikesharp".Length)]
            : Path.Combine(baseDirectory, "..", "..", "csgo", "addons", "counterstrikesharp");
        return Path.GetFullPath(Path.Combine(cssharpDirectory, "configs", "plugins", "Kzen-WeaponBalance"));
    }

    private static string DefaultText() => """
// Kzen 武器平衡。修改后重启服务器或重载插件生效。
// 备弹无限仍保留换弹动作；弹匣容量仅对以下明确配置的武器生效。
infinite_reserve_ammo = 1
// 仅将伤害倍率用于真人 CT 对僵尸 T；Boss、门和地图实体不会受影响。
zombie_only_damage = 1

// 写法：武器名.属性 = 数值
// 属性：enabled（0/1）、clip（弹匣）、price（!weapon / !kbuy 价格）、damage_scale（对僵尸伤害倍率）、knockback_scale（供 CS2Fixes weapons.cfg 手动填写的参考值）。
ak47.enabled = 1
ak47.clip = 45
ak47.price = 3200
ak47.damage_scale = 1.15
ak47.knockback_scale = 0.90

m4a1.enabled = 1
m4a1.clip = 50
m4a1.price = 3000
m4a1.damage_scale = 1.05
m4a1.knockback_scale = 1.00

p90.enabled = 1
p90.clip = 75
p90.price = 2500
p90.damage_scale = 0.75
p90.knockback_scale = 1.30

negev.enabled = 1
negev.clip = 200
negev.price = 4500
negev.damage_scale = 0.70
negev.knockback_scale = 1.40

awp.enabled = 1
awp.clip = 10
awp.price = 6000
awp.damage_scale = 2.20
awp.knockback_scale = 2.20
""";
}

internal sealed class WeaponBalance
{
    public bool Enabled { get; private set; } = true;
    public int Clip { get; private set; }
    public int Price { get; private set; }
    public float DamageScale { get; private set; } = 1.0f;
    public float KnockbackScale { get; private set; } = 1.0f;

    public void Apply(string property, string value)
    {
        switch (property.Trim().ToLowerInvariant())
        {
            case "enabled": Enabled = value is "1" or "true"; break;
            case "clip" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var clip): Clip = Math.Max(0, clip); break;
            case "price" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price): Price = Math.Max(0, price); break;
            case "damage_scale" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var damage): DamageScale = Math.Max(0.0f, damage); break;
            case "knockback_scale" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var knockback): KnockbackScale = Math.Max(0.0f, knockback); break;
        }
    }
}

internal sealed record PendingNativeBuy(int MoneyBefore, string Signature);

