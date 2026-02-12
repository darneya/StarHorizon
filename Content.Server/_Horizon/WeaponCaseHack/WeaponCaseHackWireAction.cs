using System.Linq;
using Content.Server.Wires;
using Content.Shared._Horizon.WeaponCaseHack;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Wires;
using Robust.Shared.Log;

namespace Content.Server._Horizon.WeaponCaseHack;

public sealed partial class WeaponCaseHackWireAction : ComponentWireAction<WeaponCaseHackComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-weapon-case-hack";

    private bool _isFirstWire;
    public override object? StatusKey => _isFirstWire ? WeaponCaseHackWireActionKey.Status : null;

    public override bool AddWire(Wire wire, int count)
    {
        _isFirstWire = count == 1;
        return true;
    }

    public override StatusLightState? GetLightState(Wire wire, WeaponCaseHackComponent comp)
    {
        if (!_isFirstWire)
            return null;

        if (!EntityManager.TryGetComponent<LockComponent>(wire.Owner, out var lockComp))
            return StatusLightState.Off;

        return lockComp.Locked ? StatusLightState.On : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, WeaponCaseHackComponent comp)
    {
        if (!EntityManager.TryGetComponent<LockComponent>(wire.Owner, out var lockComp) || !lockComp.Locked)
            return true;

        if (!EntityManager.TryGetComponent<WiresComponent>(wire.Owner, out var wiresComp))
            return true;

        var serialNumber = wiresComp.SerialNumber;
        if (string.IsNullOrEmpty(serialNumber))
            return true;

        var (cutColor, pulseColor, pulseFirst) = GetCorrectWireColors(serialNumber);

        Logger.Info($"[WeaponCaseHack] Serial: {serialNumber}, Cut: {cutColor}, Pulse: {pulseColor}, PulseFirst: {pulseFirst}, Action: Cut {wire.Color}");

        if (wire.Color == cutColor)
        {
            // Если нужно сначала пульсировать, а пульс ещё не сделан - ошибка порядка
            if (pulseFirst && !comp.PulseCompleted)
            {
                comp.CutCompleted = false;
                comp.PulseCompleted = false;
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("weapon-case-hack-wrong-order-pulse-first"), wire.Owner, user);
                return true;
            }

            comp.CutCompleted = true;

            if (comp.PulseCompleted)
            {
                EntityManager.System<LockSystem>().Unlock(wire.Owner, user);
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("weapon-case-hack-success"), wire.Owner, user);
                comp.CutCompleted = false;
                comp.PulseCompleted = false;
            }
            else
            {
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("weapon-case-hack-cut-success"), wire.Owner, user);
            }
        }
        else
        {
            comp.CutCompleted = false;
            comp.PulseCompleted = false;
            EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("weapon-case-hack-wrong-wire"), wire.Owner, user);
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, WeaponCaseHackComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, WeaponCaseHackComponent comp)
    {
        if (!EntityManager.TryGetComponent<LockComponent>(wire.Owner, out var lockComp) || !lockComp.Locked)
            return;

        if (!EntityManager.TryGetComponent<WiresComponent>(wire.Owner, out var wiresComp))
            return;

        var serialNumber = wiresComp.SerialNumber;
        if (string.IsNullOrEmpty(serialNumber))
            return;

        var (cutColor, pulseColor, pulseFirst) = GetCorrectWireColors(serialNumber);

        Logger.Info($"[WeaponCaseHack] Serial: {serialNumber}, Cut: {cutColor}, Pulse: {pulseColor}, PulseFirst: {pulseFirst}, Action: Pulse {wire.Color}");

        if (wire.Color == pulseColor)
        {
            // Если нужно сначала резать, а резка ещё не сделана - ошибка порядка
            if (!pulseFirst && !comp.CutCompleted)
            {
                comp.CutCompleted = false;
                comp.PulseCompleted = false;
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("weapon-case-hack-wrong-order-cut-first"), wire.Owner, user);
                return;
            }

            comp.PulseCompleted = true;

            if (comp.CutCompleted)
            {
                EntityManager.System<LockSystem>().Unlock(wire.Owner, user);
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("weapon-case-hack-success"), wire.Owner, user);
                comp.CutCompleted = false;
                comp.PulseCompleted = false;
            }
            else
            {
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("weapon-case-hack-pulse-success"), wire.Owner, user);
            }
        }
        else
        {
            comp.CutCompleted = false;
            comp.PulseCompleted = false;
            EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("weapon-case-hack-wrong-wire"), wire.Owner, user);
        }
    }

    private (WireColor cut, WireColor pulse, bool pulseFirst) GetCorrectWireColors(string serial)
    {
        var letters = serial.Length >= 4 ? serial[..4].ToUpperInvariant() : "";
        var digits = serial.Length >= 9 ? serial[5..9] : "";

        var digitValues = new List<int>();
        foreach (var c in digits)
        {
            if (char.IsDigit(c))
                digitValues.Add(c - '0');
        }

        var digitSum = digitValues.Sum();
        var vowels = new HashSet<char> { 'A', 'E', 'I', 'O', 'U' };
        var cleanSerial = serial.Replace("-", "");
        var vowelCount = letters.Count(c => vowels.Contains(c));

        // Rule1: Содержит буквы AnCo в любом порядке (pulseFirst)
        if (letters.Contains('A') && letters.Contains('N') && letters.Contains('C') && letters.Contains('O'))
            return (WireColor.Gray, WireColor.Purple, true);

        // Rule2: Содержит SS (cutFirst)
        if (serial.Contains("SS"))
            return (WireColor.Blue, WireColor.Red, false);

        // Rule3: Содержит NT (pulseFirst)
        if (serial.Contains("NT"))
            return (WireColor.Red, WireColor.Blue, true);

        // Rule4: Содержит 3026 (cutFirst)
        if (digits.Contains("3026") || serial.Contains("3026"))
            return (WireColor.Pink, WireColor.Gold, false);

        // Rule5: Первая и последняя буквы одинаковые (pulseFirst)
        if (letters.Length >= 4 && letters[0] == letters[3])
            return (WireColor.Purple, WireColor.Orange, true);

        // Rule6: X, Z и Y одновременно (cutFirst)
        if (letters.Contains('X') && letters.Contains('Z') && letters.Contains('Y'))
            return (WireColor.Fuchsia, WireColor.Cyan, false);

        // Rule7: Встречается слово "LUMA" (pulseFirst)
        if (serial.Contains("LUMA"))
            return (WireColor.Brown, WireColor.Gold, true);

        // Rule8: Серийник симметричен (cutFirst)
        if (IsPalindrome(cleanSerial))
            return (WireColor.Purple, WireColor.Navy, false);

        // Rule9: Все цифры одинаковые (pulseFirst)
        if (digitValues.Count >= 4 && digitValues.All(d => d == digitValues[0]))
            return (WireColor.Purple, WireColor.Green, true);

        // Rule10: Все буквы одинаковые (cutFirst)
        if (letters.Length >= 4 && letters.All(c => c == letters[0]))
            return (WireColor.Purple, WireColor.Brown, false);

        // Rule11: Все цифры простые (2,3,5,7) (pulseFirst)
        var primes = new HashSet<int> { 2, 3, 5, 7 };
        if (digitValues.Count >= 4 && digitValues.All(d => primes.Contains(d)))
            return (WireColor.Blue, WireColor.Pink, true);

        // Rule12: Первая буква гласная и последняя цифра нечётная (cutFirst)
        if (letters.Length > 0 && vowels.Contains(letters[0]) && digitValues.Count > 0 && digitValues[^1] % 2 == 1)
            return (WireColor.Red, WireColor.Green, false);

        // Rule13: Более двух одинаковых символов (3+) (pulseFirst)
        if (HasMoreThanTwoSameSymbols(cleanSerial))
            return (WireColor.Gold, WireColor.Navy, true);

        // Rule14: Первая буква и первая цифра чётные (cutFirst)
        if (letters.Length > 0 && IsEvenLetter(letters[0]) && digitValues.Count > 0 && digitValues[0] % 2 == 0)
            return (WireColor.Green, WireColor.Orange, false);

        // Rule15: Первая цифра = количество гласных (pulseFirst)
        if (digitValues.Count > 0 && digitValues[0] == vowelCount)
            return (WireColor.Pink, WireColor.Cyan, true);

        // Rule16: Первая буква = последней цифре (A=1, B=2, ...) (cutFirst)
        if (letters.Length > 0 && digitValues.Count > 0)
        {
            var letterValue = letters[0] - 'A' + 1;
            if (letterValue == digitValues[^1])
                return (WireColor.Blue, WireColor.Brown, false);
        }

        // Rule17: Есть повтор буквы (pulseFirst)
        if (letters.GroupBy(c => c).Any(g => g.Count() > 1))
            return (WireColor.Orange, WireColor.Red, true);

        // Rule18: Последняя буква раньше F (cutFirst)
        if (letters.Length >= 4 && letters[3] < 'F')
            return (WireColor.Pink, WireColor.Purple, false);

        // Rule19: Буквы идут в обратном порядке (pulseFirst)
        if (IsReverseAlphabetical(letters))
            return (WireColor.Orange, WireColor.Blue, true);

        // Rule20: Буквы идут по алфавиту (cutFirst)
        if (IsAlphabetical(letters))
            return (WireColor.Blue, WireColor.Gold, false);

        // Rule21: Есть буква Z (pulseFirst)
        if (letters.Contains('Z'))
            return (WireColor.Brown, WireColor.Fuchsia, true);

        // Rule22: Есть буква A (cutFirst)
        if (letters.Contains('A'))
            return (WireColor.Red, WireColor.Navy, false);

        // Rule23: Произведение цифр > 200 (pulseFirst)
        if (digitValues.Count > 0)
        {
            var product = digitValues.Aggregate(1, (a, b) => a * b);
            if (product > 200)
                return (WireColor.Brown, WireColor.Green, true);
        }

        // Rule24: Последняя цифра нечётная (cutFirst)
        if (digitValues.Count > 0 && digitValues[^1] % 2 == 1)
            return (WireColor.Orange, WireColor.Pink, false);

        // Rule25: Последняя цифра чётная (pulseFirst)
        if (digitValues.Count > 0 && digitValues[^1] % 2 == 0)
            return (WireColor.Red, WireColor.Cyan, true);

        // Rule26: Первая цифра больше последней (cutFirst)
        if (digitValues.Count >= 2 && digitValues[0] > digitValues[^1])
            return (WireColor.Blue, WireColor.Orange, false);

        // Rule27: Цифры в обратном порядке (pulseFirst)
        if (IsReverseOrdered(digitValues))
            return (WireColor.Fuchsia, WireColor.Red, true);

        // Rule28: Все цифры разные (cutFirst)
        if (digitValues.Distinct().Count() == digitValues.Count && digitValues.Count >= 4)
            return (WireColor.Cyan, WireColor.Brown, false);

        // Rule29: Две одинаковые цифры подряд (pulseFirst)
        for (int i = 0; i < digitValues.Count - 1; i++)
        {
            if (digitValues[i] == digitValues[i + 1])
                return (WireColor.Navy, WireColor.Gold, true);
        }

        // Rule30: Есть цифра 9 (cutFirst)
        if (digitValues.Contains(9))
            return (WireColor.Green, WireColor.Purple, false);

        // Rule31: Есть цифра 0 (pulseFirst)
        if (digitValues.Contains(0))
            return (WireColor.Orange, WireColor.Navy, true);

        // Rule32: Первая буква позже M (cutFirst)
        if (letters.Length > 0 && letters[0] > 'M')
            return (WireColor.Cyan, WireColor.Blue, false);

        // Rule33: Сумма цифр делится на 3 (pulseFirst)
        if (digitSum % 3 == 0)
            return (WireColor.Gray, WireColor.Red, true);

        // Rule34: Содержит "ERP" (cutFirst)
        if (serial.Contains("ERP"))
            return (WireColor.Pink, WireColor.Brown, false);

        // Rule35: Сумма цифр < 10 (pulseFirst)
        if (digitSum < 10)
            return (WireColor.Gold, WireColor.Fuchsia, true);

        // Rule36: Сумма цифр > 25 (cutFirst)
        if (digitSum > 25)
            return (WireColor.Brown, WireColor.Pink, false);

        // Rule37: Сумма цифр нечётная (pulseFirst)
        if (digitSum % 2 == 1)
            return (WireColor.Red, WireColor.Gold, true);

        // Rule38: Сумма цифр чётная (cutFirst)
        if (digitSum % 2 == 0)
            return (WireColor.Blue, WireColor.Green, false);

        return (WireColor.Navy, WireColor.Gray, false);
    }

    private static bool IsPalindrome(string s)
    {
        for (int i = 0; i < s.Length / 2; i++)
        {
            if (s[i] != s[s.Length - 1 - i])
                return false;
        }
        return true;
    }

    private static bool HasMoreThanTwoSameSymbols(string s)
    {
        return s.GroupBy(c => c).Any(g => g.Count() > 2);
    }

    private static bool IsEvenLetter(char c)
    {
        var pos = c - 'A' + 1;
        return pos % 2 == 0;
    }

    private static bool IsAlphabetical(string s)
    {
        if (s.Length < 2)
            return false;
        for (int i = 0; i < s.Length - 1; i++)
        {
            if (s[i] >= s[i + 1])
                return false;
        }
        return true;
    }

    private static bool IsReverseAlphabetical(string s)
    {
        if (s.Length < 2)
            return false;
        for (int i = 0; i < s.Length - 1; i++)
        {
            if (s[i] <= s[i + 1])
                return false;
        }
        return true;
    }

    private static bool IsReverseOrdered(List<int> digits)
    {
        if (digits.Count < 2)
            return false;
        for (int i = 0; i < digits.Count - 1; i++)
        {
            if (digits[i] <= digits[i + 1])
                return false;
        }
        return true;
    }
}
