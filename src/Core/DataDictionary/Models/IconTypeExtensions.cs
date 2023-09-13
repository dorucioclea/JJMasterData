using JJMasterData.Commons.Exceptions;

namespace JJMasterData.Core.DataDictionary;

public static class IconTypeExtensions
{
    public static string GetCssClass(this IconType icon)
    {
        if ((int)icon <= 691)
        {
            var description = PascalToParamCase(icon.ToString());
            return $"fa fa-{description}";
        }

        var iconString = icon.ToString();

        if (iconString.StartsWith("Solid"))
        {
            return $"fa-solid fa-{PascalToParamCase(icon.ToString().Replace("Solid", string.Empty))}";
        }
        if (iconString.StartsWith("Regular"))
        {
            return $"fa-regular fa-{PascalToParamCase(icon.ToString().Replace("Regular", string.Empty))}";
        }
        if (iconString.StartsWith("Brands"))
        {
            return $"fa-brands fa-{PascalToParamCase(icon.ToString().Replace("Brands", string.Empty))}";
        }

        throw new JJMasterDataException("Invalid IconType");
    }

    public static string GetDescription(this IconType icon)
    {
        return PascalToParamCase(icon.ToString());
    }

    public static int GetId(this IconType icon)
    {
        return (int)icon;
    }

    public static string PascalToParamCase(string icon)
    {
        int i = 0;
        string parsedName = string.Empty;
        foreach (char c in icon)
        {
            i++;
            if (i == 1)
            {
                parsedName += c;
                continue;
            }

            if (c == '_')
            {
                i = 0;
                continue;
            }

            if (char.IsUpper(c) || char.IsDigit(c))
            {
                parsedName += '-';
            }

            parsedName += c;
        }

        return parsedName.ToLower();
    }
}