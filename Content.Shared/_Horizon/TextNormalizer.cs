using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Content.Shared._Horizon;

/// <summary>
/// Класс нормализующий строки убирая цветовые коды.
/// В будущем возможно будут другие функции форматирования строк.
/// </summary>
public class TextNormalizer
{
    /// <summary>
    /// Преобразует текст с цветным форматированием,
    /// удаляя теги цветов для корректного отображения.
    /// Пример:
    ///     До: [color=#4F4F4F]T[/color][color=#FCD05C]est[/color]
    ///     После: Test
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string GetFormatting(string text)
    {
        return Regex.Replace(text, @"\[/?color[^\]]*\]", "");
    }
}
