#include "PluginLocalization.h"

namespace
{
juce::String translatedText (dlm::PluginLanguage language, const juce::String& englishText)
{
    struct Entry
    {
        const char* english;
        const char* chinese;
        const char* japanese;
        const char* russian;
    };

    static constexpr Entry entries[]
    {
        { "Source name", "信号源名称", "ソース名", "Имя источника" },
        { "Mute", "静音", "ミュート", "Отключить звук" },
        { "Muted", "已静音", "ミュート中", "Звук отключен" },
        { "Start uMeter", "启动 uMeter", "uMeter を起動", "Запустить uMeter" },
        { "Sender stopped", "发送器已停止", "送信を停止しました", "Передатчик остановлен" },
        { "Connected to uMeter", "已连接到 uMeter", "uMeter に接続済み", "Подключено к uMeter" },
        { "Sending, no recent reply", "正在发送，最近无响应", "送信中、最近の応答なし", "Отправка без недавнего ответа" },
        { "Waiting for uMeter", "正在等待 uMeter", "uMeter を待機中", "Ожидание uMeter" },
        { "dropped", "丢弃", "ドロップ", "потеряно" },
        { "uMeter app was not found.", "未找到 uMeter 应用。", "uMeter アプリが見つかりません。", "Приложение uMeter не найдено." },
        { "uMeter could not be started.", "无法启动 uMeter。", "uMeter を起動できませんでした。", "Не удалось запустить uMeter." },
        { "uMeter started.", "uMeter 已启动。", "uMeter を起動しました。", "uMeter запущен." },
        { "uMeter is already running.", "uMeter 已在运行。", "uMeter はすでに実行中です。", "uMeter уже запущен." }
    };

    for (const auto& entry : entries)
    {
        if (englishText == entry.english)
        {
            switch (language)
            {
                case dlm::PluginLanguage::Chinese:  return juce::String::fromUTF8 (entry.chinese);
                case dlm::PluginLanguage::Japanese: return juce::String::fromUTF8 (entry.japanese);
                case dlm::PluginLanguage::Russian:  return juce::String::fromUTF8 (entry.russian);
                case dlm::PluginLanguage::English:
                default:                            return englishText;
            }
        }
    }

    return englishText;
}
}

namespace dlm
{
PluginLocalization::PluginLocalization()
    : PluginLocalization (resolveLanguage (loadPreferenceCode (defaultSettingsFile()),
                                           juce::SystemStats::getDisplayLanguage()))
{
}

PluginLocalization::PluginLocalization (PluginLanguage selectedLanguage)
    : language (selectedLanguage)
{
}

juce::String PluginLocalization::translate (const juce::String& englishText) const
{
    return translatedText (language, englishText);
}

PluginLanguage PluginLocalization::resolveLanguage (const juce::String& preferenceCode,
                                                    const juce::String& displayLanguage)
{
    auto code = preferenceCode.trim();
    if (code.isEmpty() || code.equalsIgnoreCase ("auto"))
    {
        code = displayLanguage.trim();
    }

    if (code.startsWithIgnoreCase ("zh"))
    {
        return PluginLanguage::Chinese;
    }

    if (code.startsWithIgnoreCase ("ja"))
    {
        return PluginLanguage::Japanese;
    }

    if (code.startsWithIgnoreCase ("ru"))
    {
        return PluginLanguage::Russian;
    }

    return PluginLanguage::English;
}

juce::String PluginLocalization::loadPreferenceCode (const juce::File& settingsFile)
{
    if (! settingsFile.existsAsFile())
    {
        return "auto";
    }

    const auto parsed = juce::JSON::parse (settingsFile.loadFileAsString());
    if (const auto* object = parsed.getDynamicObject())
    {
        return object->getProperty ("LanguageCode").toString();
    }

    return "auto";
}

juce::File PluginLocalization::defaultSettingsFile()
{
    return juce::File::getSpecialLocation (juce::File::userApplicationDataDirectory)
        .getChildFile ("uMeter")
        .getChildFile ("settings.json");
}
}
