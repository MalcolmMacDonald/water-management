using UnityEditor;
using UnityEngine;

public class GridTools
{
    private const string MenuName = "Grid Tools/Update Grid On Save";
    private const string SettingName = "GenerateCollisionOnSave";

    public static bool IsEnabled
    {
        get => EditorPrefs.GetBool(SettingName, true);
        set => EditorPrefs.SetBool(SettingName, value);
    }

    [MenuItem(MenuName)]
    public static void ToggleUpdateGridOnSave()
    {
        IsEnabled = !IsEnabled;
    }

    [MenuItem(MenuName, true)]
    private static bool ToggleActionValidate()
    {
        Menu.SetChecked(MenuName, IsEnabled);
        return true;
    }

    [MenuItem("Grid Tools/Generate Grid", priority = 1)]
    private static void GenerateGrid()
    {
        Object.FindObjectOfType<PipesGrid>().GenerateGrid();
    }
}