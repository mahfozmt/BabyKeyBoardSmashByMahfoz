using System;
using System.Reflection;
using System.Windows.Input;
using BabySmashBN.Services;

namespace BabySmashBN.Tests;

public class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("Running BabySmashBN Unit Tests...");
        var tests = new KeyMapServiceTests();
        int passed = 0;
        int failed = 0;

        void Run(string name, Action action)
        {
            try
            {
                action();
                Console.WriteLine($"  [PASS] {name}");
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [FAIL] {name}: {ex.Message}");
                failed++;
            }
        }

        // Test Digits
        Run("Key_Digits_Map_To_Bangla_Numerals (0->০)", () => tests.Key_Digits_Map_Correctly_To_Bangla_Numerals(Key.D0, "০"));
        Run("Key_Digits_Map_To_Bangla_Numerals (1->১)", () => tests.Key_Digits_Map_Correctly_To_Bangla_Numerals(Key.D1, "১"));
        Run("Key_Digits_Map_To_Bangla_Numerals (3->৩)", () => tests.Key_Digits_Map_Correctly_To_Bangla_Numerals(Key.D3, "৩"));
        Run("Key_Digits_Map_To_Bangla_Numerals (9->৯)", () => tests.Key_Digits_Map_Correctly_To_Bangla_Numerals(Key.D9, "৯"));

        // Test Letters
        Run("Key_Letters_Map_To_Bangla_Characters (A->অ)", () => tests.Key_Letters_Map_Correctly_To_Bangla_Characters(Key.A, "অ"));
        Run("Key_Letters_Map_To_Bangla_Characters (B->ব)", () => tests.Key_Letters_Map_Correctly_To_Bangla_Characters(Key.B, "ব"));
        Run("Key_Letters_Map_To_Bangla_Characters (K->ক)", () => tests.Key_Letters_Map_Correctly_To_Bangla_Characters(Key.K, "ক"));
        Run("Key_Letters_Map_To_Bangla_Characters (M->ম)", () => tests.Key_Letters_Map_Correctly_To_Bangla_Characters(Key.M, "ম"));

        // Test Virtual Keys
        Run("VirtualKey_Digits_Map_Correctly", () => tests.VirtualKey_Digits_Map_Correctly());
        Run("VirtualKey_Non_Alphanumeric_Returns_Shape", () => tests.VirtualKey_Non_Alphanumeric_Returns_Shape());

        // Test Non-Alphanumeric Keys -> Shapes with Funny Sounds!
        Run("Non_Alphanumeric_Keys_Return_Shapes (Space)", () => tests.Non_Alphanumeric_Keys_Return_Shapes_With_Funny_Sounds(Key.Space));
        Run("Non_Alphanumeric_Keys_Return_Shapes (Return)", () => tests.Non_Alphanumeric_Keys_Return_Shapes_With_Funny_Sounds(Key.Return));
        Run("Non_Alphanumeric_Keys_Return_Shapes (Back)", () => tests.Non_Alphanumeric_Keys_Return_Shapes_With_Funny_Sounds(Key.Back));
        Run("Non_Alphanumeric_Keys_Return_Shapes (Tab)", () => tests.Non_Alphanumeric_Keys_Return_Shapes_With_Funny_Sounds(Key.Tab));
        Run("Non_Alphanumeric_Keys_Return_Shapes (Left)", () => tests.Non_Alphanumeric_Keys_Return_Shapes_With_Funny_Sounds(Key.Left));
        Run("Non_Alphanumeric_Keys_Return_Shapes (F5)", () => tests.Non_Alphanumeric_Keys_Return_Shapes_With_Funny_Sounds(Key.F5));

        // Test Colors
        Run("Colors_Are_Bright_And_Valid", () => tests.Colors_Are_Bright_And_Valid());

        Console.WriteLine($"\nResults: {passed} passed, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }
}
