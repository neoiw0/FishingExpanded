using System;
using StardewModdingAPI;

namespace FishingExpanded.Services
{
    /// <summary>Generic Mod Config Menu 可选 API（签名与已安装 GMCM 1.16.0 的 IGenericModConfigMenuApi 一致；仅声明用到的成员；未安装 GMCM 时 GetApi 返回 null，不注册菜单）。</summary>
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);

        void AddParagraph(IManifest mod, Func<string> text);

        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
    }
}
