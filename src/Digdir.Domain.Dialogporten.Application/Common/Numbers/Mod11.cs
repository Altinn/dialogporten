﻿using System.Diagnostics.CodeAnalysis;

namespace Digdir.Domain.Dialogporten.Application.Common.Numbers;

internal static class Mod11
{
    private const int Mod11Number = 11;

    public static bool TryCalculateControlDigit(ReadOnlySpan<char> number, int[] weights, [NotNullWhen(true)] out int? controlDigit)
    {
        var digits = number.ExtractDigits();

        if (digits.Length != number.Length ||
            digits.Length > weights.Length)
        {
            controlDigit = null;
            return false;
        }

        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += digits[i] * weights[i];
        }
        controlDigit = Mod11Number - (sum % Mod11Number);
        return true;
    }

    private static int[] ExtractDigits(this ReadOnlySpan<char> number)
    {
        var result = new int[number.Length];
        var index = 0;

        foreach (var character in number)
        {
            if (char.IsDigit(character))
            {
                result[index++] = character - '0';
            }
        }

        Array.Resize(ref result, index);
        return result;
    }
}
