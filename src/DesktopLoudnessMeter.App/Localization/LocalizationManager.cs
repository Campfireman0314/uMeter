using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using DesktopLoudnessMeter.App.Controls;

namespace DesktopLoudnessMeter.App.Localization;

public static class LocalizationManager
{
    public const string AutomaticLanguageCode = "auto";
    public const string EnglishLanguageCode = "en";
    public const string ChineseLanguageCode = "zh-CN";
    public const string JapaneseLanguageCode = "ja";
    public const string RussianLanguageCode = "ru";

    private static readonly TranslationEntry[] Entries =
    [
        new("Global Settings", "全局设置", "全般設定", "Общие настройки"),
        new("Events, output, and defaults", "事件、输出与默认值", "イベント、出力、既定値", "События, вывод и значения по умолчанию"),
        new("Language", "语言", "言語", "Язык"),
        new("Use the system language", "使用系统语言", "システム言語を使用", "Использовать язык системы"),
        new("Maximum event entries", "最大事件条数", "イベント履歴の上限", "Максимум событий"),
        new("Stored recent-event history size. Valid range: 1 to 200.", "最近事件的保存条数。有效范围：1 到 200。", "最近のイベント履歴の保存数。範囲: 1～200。", "Число сохраненных недавних событий. Допустимо: от 1 до 200."),
        new("Event output folder", "事件输出文件夹", "イベント出力フォルダー", "Папка вывода событий"),
        new("Load default detail layout from file", "从文件加载默认详细布局", "ファイルから既定の詳細レイアウトを読み込む", "Загружать макет подробного вида из файла"),
        new("Start uMeter with Windows", "随 Windows 启动 uMeter", "Windows 起動時に uMeter を開始", "Запускать uMeter вместе с Windows"),
        new("Automatically start uMeter from sender plugins", "由发送器插件自动启动 uMeter", "送信プラグインから uMeter を自動起動", "Автоматически запускать uMeter из плагинов-передатчиков"),
        new("Hide to tray after automatic startup", "自动启动后隐藏到托盘", "自動起動後に通知領域へ隠す", "После автоматического запуска скрывать в области уведомлений"),
        new("Show hints for detail meters and diagrams", "显示详细仪表和图表提示", "詳細メーターとグラフのヒントを表示", "Показывать подсказки для подробных индикаторов и графиков"),
        new("Browse", "浏览", "参照", "Обзор"),
        new("Close", "关闭", "閉じる", "Закрыть"),
        new("Back", "返回", "戻る", "Назад"),
        new("Details", "详细信息", "詳細", "Детали"),
        new("Reset", "重置", "リセット", "Сброс"),
        new("Reset All", "全部重置", "すべてリセット", "Сбросить все"),
        new("Forget", "忘记", "登録解除", "Забыть"),
        new("Hide", "隐藏", "隠す", "Скрыть"),
        new("Restore", "恢复", "復元", "Восстановить"),
        new("Show hidden", "显示隐藏项", "非表示を表示", "Показать скрытые"),
        new("Always on top", "始终置顶", "常に手前に表示", "Поверх всех окон"),
        new("Eco mode", "节能模式", "省電力モード", "Экономичный режим"),
        new("Pop-up", "弹出窗口", "ポップアップ", "Окно"),
        new("Detail pop-up", "详细信息弹窗", "詳細ポップアップ", "Подробное всплывающее окно"),
        new("Switch to...", "切换到...", "切り替え...", "Выбрать..."),
        new("Top", "顶部", "先頭", "В начало"),
        new("Move up", "上移", "上へ移動", "Переместить вверх"),
        new("Move down", "下移", "下へ移動", "Переместить вниз"),
        new("Drag to reorder", "拖动以重新排序", "ドラッグして並べ替え", "Перетащите для изменения порядка"),
        new("Global settings", "全局设置", "全般設定", "Общие настройки"),
        new("Hide to tray", "隐藏到托盘", "通知領域へ隠す", "Скрыть в области уведомлений"),
        new("WASAPI / VST3 source metering", "WASAPI / VST3 信号源测量", "WASAPI / VST3 ソース計測", "Измерение источников WASAPI / VST3"),
        new("Signal Shuffle", "信号消消乐", "シグナルシャッフル", "Сигнальный микс"),
        new("Audio tile game", "音频三消游戏", "オーディオタイルゲーム", "Аудиоигра с плитками"),
        new("Audio source", "音频源", "オーディオソース", "Источник звука"),
        new("Restart board", "重新开始", "ボードをやり直す", "Начать поле заново"),
        new("New board", "新棋盘", "新しいボード", "Новое поле"),
        new("New game", "新游戏", "新しいゲーム", "Новая игра"),
        new("Cannon: clear tray", "大炮：清空槽位", "大砲：トレイを消去", "Пушка: очистить лоток"),
        new("Fan: shuffle table", "扇子：重排桌面", "扇子：場をシャッフル", "Веер: перемешать стол"),
        new("Digger: add tray slot", "铲子：增加一个槽位", "シャベル：トレイ枠を追加", "Лопата: добавить ячейку"),
        new("Show live beat debug", "显示实时节拍调试", "ライブビートデバッグを表示", "Показать отладку ритма"),
        new("Live beat debug", "实时节拍调试", "ライブビートデバッグ", "Отладка ритма"),
        new("Bass energy", "低频能量", "低域エネルギー", "Энергия баса"),
        new("Signal energy", "全频能量", "信号エネルギー", "Энергия сигнала"),
        new("Spectral flux", "频谱通量", "スペクトルフラックス", "Спектральный поток"),
        new("Band agreement", "频带一致性", "帯域一致度", "Согласованность полос"),
        new("Periodicity", "周期性", "周期性", "Периодичность"),
        new("Adaptive baseline", "自适应基线", "適応ベースライン", "Адаптивный фон"),
        new("Trigger threshold", "触发阈值", "トリガーしきい値", "Порог срабатывания"),
        new("Onset strength", "瞬态强度", "オンセット強度", "Сила атаки"),
        new("Beat pulse", "节拍脉冲", "ビートパルス", "Импульс ритма"),
        new("Sensitivity", "灵敏度", "感度", "Чувствительность"),
        new("Groove", "律动", "グルーヴ", "Грув"),
        new("On beat", "踩中节拍", "オンビート", "В ритм"),
        new("Perfect", "完美", "パーフェクト", "Идеально"),
        new("Playing", "游戏中", "プレイ中", "Игра"),
        new("Tool lock", "道具锁", "ツールロック", "Блокировка инструментом"),
        new("Board cleared!", "已清空棋盘！", "ボードクリア！", "Поле очищено!"),
        new("Tray full", "槽位已满", "トレイがいっぱいです", "Лоток заполнен"),
        new("{0} tiles", "剩余 {0} 张", "残り {0} タイル", "Осталось плиток: {0}"),
        new("{0} matches", "已消除 {0} 组", "{0} 組マッチ", "Совпадений: {0}"),
        new("Waiting for audio sources", "正在等待音频源", "オーディオソースを待機中", "Ожидание источников звука"),
        new("No active audio sources", "没有活动音频源", "アクティブなオーディオソースはありません", "Нет активных источников звука"),
        new("1 source tracked", "正在跟踪 1 个信号源", "1 個のソースを追跡中", "Отслеживается 1 источник"),
        new("{0} sources tracked", "正在跟踪 {0} 个信号源", "{0} 個のソースを追跡中", "Отслеживается источников: {0}"),
        new("Refresh failed: {0}", "刷新失败：{0}", "更新に失敗しました: {0}", "Ошибка обновления: {0}"),
        new("Instance not found", "未找到实例", "インスタンスが見つかりません", "Экземпляр не найден"),
        new("Per-process LUFS metrics warm up after active audio starts.", "每个进程的 LUFS 测量会在活动音频开始后逐步稳定。", "プロセスごとの LUFS 計測は音声開始後に安定します。", "Измерения LUFS для процесса стабилизируются после начала воспроизведения."),
        new("Performance", "性能", "パフォーマンス", "Производительность"),
        new("Recent", "最近", "最近", "Недавние"),
        new("No matching events", "没有匹配的事件", "一致するイベントはありません", "Нет подходящих событий"),
        new("All", "全部", "すべて", "Все"),
        new("Sources", "信号源", "ソース", "Источники"),
        new("Peaks", "峰值", "ピーク", "Пики"),
        new("Loudness", "响度", "ラウドネス", "Громкость"),
        new("Overlap", "重叠", "重なり", "Перекрытие"),
        new("Global", "全局", "全体", "Все источники"),
        new("Source appeared", "信号源已出现", "ソースが現れました", "Источник появился"),
        new("Source disappeared", "信号源已消失", "ソースが消えました", "Источник исчез"),
        new("True peak max", "真峰值最大值", "トゥルーピーク最大値", "Максимум истинного пика"),
        new("Loudness jump", "响度突变", "ラウドネスの急変", "Скачок громкости"),
        new("Possible overlap", "可能存在重叠", "重なりの可能性", "Возможное перекрытие"),
        new("Mode", "模式", "モード", "Режим"),
        new("Peak", "峰值", "ピーク", "Пик"),
        new("LUFS", "LUFS", "LUFS", "LUFS"),
        new("Overall", "整体", "全体", "Общий"),
        new("Mid", "中置", "Mid", "Mid"),
        new("Side", "侧向", "Side", "Side"),
        new("Channels", "分声道", "チャンネル別", "По каналам"),
        new("Loopback", "环回", "ループバック", "Обратная петля"),
        new("Plugin", "插件", "プラグイン", "Плагин"),
        new("Unknown", "未知", "不明", "Неизвестно"),
        new("Logarithmic", "对数", "対数", "Логарифмическая"),
        new("Mel", "梅尔", "メル", "Мел"),
        new("Momentary loudness", "瞬时响度", "モーメンタリーラウドネス", "Мгновенная громкость"),
        new("Short-term loudness", "短期响度", "ショートタームラウドネス", "Кратковременная громкость"),
        new("Integrated loudness", "综合响度", "インテグレーテッドラウドネス", "Интегральная громкость"),
        new("True-peak level", "真峰值电平", "トゥルーピークレベル", "Уровень истинного пика"),
        new("100 ms RMS level", "100 毫秒 RMS 电平", "100 ms RMS レベル", "Уровень RMS за 100 мс"),
        new("Scale", "刻度", "スケール", "Шкала"),
        new("Range", "范围", "範囲", "Диапазон"),
        new("Warning", "警告", "警告", "Предупреждение"),
        new("Cycle", "周期", "周期", "Цикл"),
        new("Clock", "响度时钟", "ラウドネスクロック", "Часы громкости"),
        new("Waveform", "波形", "波形", "Форма волны"),
        new("Spectrum", "频谱", "スペクトラム", "Спектр"),
        new("Stereo image", "立体声像", "ステレオイメージ", "Стереополе"),
        new("Stereo Scale", "立体声刻度", "ステレオスケール", "Шкала стереополя"),
        new("Frequency collision", "频率冲突", "周波数衝突", "Частотные конфликты"),
        new("True Peak", "真峰值", "トゥルーピーク", "Истинный пик"),
        new("{0}x oversampling", "{0} 倍过采样", "{0} 倍オーバーサンプリング", "{0}× оверсэмплинг"),
        new("Correlation", "相关度", "相関", "Корреляция"),
        new("Active", "活动中", "アクティブ", "Активен"),
        new("Inactive", "非活动", "非アクティブ", "Неактивен"),
        new("Unknown session", "未知会话", "不明なセッション", "Неизвестный сеанс"),
        new("{0} source", "{0} 信号源", "{0} ソース", "Источник {0}"),
        new("{0} source - {1}", "{0} 信号源 - {1}", "{0} ソース - {1}", "Источник {0} - {1}"),
        new("{0} source - PID {1}", "{0} 信号源 - PID {1}", "{0} ソース - PID {1}", "Источник {0} - PID {1}"),
        new("Not selected", "未选择", "未選択", "Не выбрано"),
        new("Showing hidden", "正在显示隐藏项", "非表示項目を表示中", "Скрытые показаны"),
        new("Hidden filtered", "已过滤隐藏项", "非表示項目を除外中", "Скрытые отфильтрованы"),
        new("{0} to {1} dB", "{0} 至 {1} dB", "{0} ～ {1} dB", "от {0} до {1} дБ"),
        new("{0} to {1} LUFS", "{0} 至 {1} LUFS", "{0} ～ {1} LUFS", "от {0} до {1} LUFS"),
        new("n/a", "不可用", "利用不可", "н/д"),
        new("max {0}", "最大 {0}", "最大 {0}", "макс. {0}"),
        new("Scale range", "刻度范围", "スケール範囲", "Диапазон шкалы"),
        new("Manual top", "手动上限", "手動上限", "Верхняя граница вручную"),
        new("Auto scale", "自动缩放", "自動スケール", "Автомасштаб"),
        new("Auto lower limit", "自动下限", "自動下限", "Нижний предел автомасштаба"),
        new("Auto speed", "自动调整速度", "自動調整速度", "Скорость автомасштаба"),
        new("Trace persistence", "轨迹保留时间", "軌跡の残存時間", "Послесвечение трассы"),
        new("Level floor", "电平下限", "レベル下限", "Нижний порог уровня"),
        new("Prominence", "显著度", "突出度", "Выраженность"),
        new("Match tolerance", "匹配容差", "一致許容値", "Допуск совпадения"),
        new("The loudness clock traces momentary loudness around a repeating time cycle, making sustained level and recent changes easy to compare.", "响度时钟按重复的时间周期描绘瞬时响度，便于比较持续电平和近期变化。", "ラウドネスクロックは反復する時間周期に沿ってモーメンタリーラウドネスを描き、持続レベルと直近の変化を比較しやすくします。", "Часы громкости отображают мгновенную громкость по повторяющемуся временному циклу, упрощая сравнение устойчивого уровня и недавних изменений."),
        new("Waveform scrolls recent sample amplitude over time. Switch between overall and separate left/right channels in its settings.", "波形图随时间滚动显示最近的采样幅度，可在设置中切换整体或独立左右声道。", "波形は直近のサンプル振幅を時間に沿ってスクロール表示します。設定で全体表示と左右別表示を切り替えられます。", "Форма волны показывает недавнюю амплитуду во времени. В настройках можно выбрать общий вид или отдельные левый и правый каналы."),
        new("Spectrum scrolls frequency energy over time, with low frequencies at the left and high frequencies at the right.", "频谱图随时间滚动显示频率能量，左侧为低频，右侧为高频。", "スペクトラムは周波数エネルギーを時間に沿って表示し、左が低域、右が高域です。", "Спектр показывает энергию частот во времени: низкие частоты слева, высокие справа."),
        new("Stereo image plots left/right energy as a fan and correlation from -1 (opposed phase) through 0 (wide) to +1 (mono-compatible).", "立体声像以扇形显示左右声道能量，相关度从 -1（反相）经 0（宽）到 +1（单声道兼容）。", "ステレオイメージは左右のエネルギーを扇形で示し、相関を -1（逆相）から 0（ワイド）、+1（モノ互換）まで表示します。", "Стереополе показывает энергию левого и правого каналов веером и корреляцию от -1 (противофаза) через 0 (широкое поле) до +1 (моносовместимость)."),
        new("Frequency collision highlights bands where this source and the selected comparison source have strong overlapping spectrum energy.", "频率冲突会突出显示当前信号源与所选对比信号源频谱能量强烈重叠的频段。", "周波数衝突は、このソースと選択した比較ソースのスペクトルエネルギーが強く重なる帯域を強調します。", "Частотные конфликты выделяют полосы, где энергия спектра этого и выбранного для сравнения источника заметно перекрывается."),
        new("Momentary loudness shows the K-weighted loudness over the latest 400 ms. Mid = (L + R) / 2 and Side = (L - R) / 2.", "瞬时响度显示最近 400 毫秒的 K 加权响度。Mid = (L + R) / 2，Side = (L - R) / 2。", "モーメンタリーラウドネスは直近 400 ms の K 特性ラウドネスです。Mid = (L + R) / 2、Side = (L - R) / 2。", "Мгновенная громкость показывает K-взвешенное значение за последние 400 мс. Mid = (L + R) / 2, Side = (L - R) / 2."),
        new("Short-term loudness averages the selected signal over the latest 3 seconds for a steadier program-level view.", "短期响度对所选信号最近 3 秒取平均，以提供更稳定的节目电平视图。", "ショートタームラウドネスは選択信号の直近 3 秒を平均し、より安定した番組レベルを示します。", "Кратковременная громкость усредняет выбранный сигнал за последние 3 секунды для более устойчивой оценки уровня программы."),
        new("Integrated loudness applies BS.1770-style absolute and relative gating across the selected signal since the last reset.", "综合响度对上次重置以来的所选信号应用 BS.1770 风格的绝对和相对门限。", "インテグレーテッドラウドネスは前回のリセット以降の選択信号に BS.1770 方式の絶対・相対ゲートを適用します。", "Интегральная громкость применяет абсолютный и относительный гейтинг по BS.1770 к выбранному сигналу с момента последнего сброса."),
        new("True peak estimates inter-sample peaks over the latest 100 ms. The max readout holds the highest selected-signal value until reset.", "真峰值估算最近 100 毫秒的采样间峰值；最大值会保持所选信号的最高读数，直至重置。", "トゥルーピークは直近 100 ms のサンプル間ピークを推定します。最大値はリセットまで選択信号の最高値を保持します。", "Истинный пик оценивает межсемпловые пики за последние 100 мс. Максимум удерживает наибольшее значение выбранного сигнала до сброса."),
        new("100 ms RMS shows the recent average signal energy. Use Mid to inspect centered content and Side to inspect stereo difference content.", "100 毫秒 RMS 显示近期平均信号能量。使用 Mid 检查居中内容，使用 Side 检查立体声差信号。", "100 ms RMS は直近の平均信号エネルギーです。Mid で中央成分、Side でステレオ差分成分を確認できます。", "RMS за 100 мс показывает среднюю энергию сигнала. Mid позволяет проверить центральное содержимое, Side — разностную стереосоставляющую."),
        new("Clock settings", "响度时钟设置", "ラウドネスクロック設定", "Настройки часов громкости"),
        new("Waveform settings", "波形设置", "波形設定", "Настройки формы волны"),
        new("Spectrum settings", "频谱设置", "スペクトラム設定", "Настройки спектра"),
        new("Stereo image scale settings", "立体声像刻度设置", "ステレオイメージのスケール設定", "Настройки шкалы стереополя"),
        new("Collision settings", "冲突检测设置", "衝突検出設定", "Настройки конфликтов"),
        new("Show clock", "显示响度时钟", "ラウドネスクロックを表示", "Показать часы громкости"),
        new("Show waveform", "显示波形", "波形を表示", "Показать форму волны"),
        new("Show spectrum", "显示频谱", "スペクトラムを表示", "Показать спектр"),
        new("Show stereo image", "显示立体声像", "ステレオイメージを表示", "Показать стереополе"),
        new("Show frequency collision", "显示频率冲突", "周波数衝突を表示", "Показать частотные конфликты"),
        new("Save detail layout", "保存详细布局", "詳細レイアウトを保存", "Сохранить подробный макет"),
        new("Load detail layout", "加载详细布局", "詳細レイアウトを読み込む", "Загрузить подробный макет"),
        new("Double click to edit alias", "双击编辑别名", "ダブルクリックして別名を編集", "Дважды щелкните для изменения псевдонима"),
        new("Forget saved alias and detail settings for this source.", "忘记此信号源已保存的别名和详细设置。", "このソースの保存済み別名と詳細設定を消去します。", "Удалить сохраненный псевдоним и подробные настройки этого источника."),
        new("Lowers CPU use by slowing UI refresh and spectrum analysis. Best for older PCs or long sessions.", "通过降低界面刷新和频谱分析速度来减少 CPU 占用，适合较旧电脑或长时间使用。", "UI 更新とスペクトル解析を遅くして CPU 使用率を下げます。古い PC や長時間のセッションに適しています。", "Снижает нагрузку на процессор за счет более редкого обновления интерфейса и анализа спектра. Полезно на старых ПК и при долгой работе."),
        new("Show uMeter", "显示 uMeter", "uMeter を表示", "Показать uMeter"),
        new("Pop-up App...", "弹出应用窗口...", "アプリをポップアップ...", "Открыть приложение во всплывающем окне..."),
        new("(no active apps)", "（没有活动应用）", "（アクティブなアプリなし）", "(нет активных приложений)"),
        new("Exit", "退出", "終了", "Выход"),
        new("Choose default detail layout", "选择默认详细布局", "既定の詳細レイアウトを選択", "Выберите подробный макет по умолчанию"),
        new("Built-in default detail layout is active.", "正在使用内置默认详细布局。", "組み込みの既定レイアウトを使用中です。", "Используется встроенный подробный макет."),
        new("Choose a detail layout file or built-in defaults will be used.", "请选择详细布局文件，否则将使用内置默认值。", "詳細レイアウトファイルを選択してください。未選択時は組み込み設定を使用します。", "Выберите файл подробного макета, иначе будет использован встроенный макет."),
        new("Default detail layout file is valid.", "默认详细布局文件有效。", "既定の詳細レイアウトファイルは有効です。", "Файл подробного макета по умолчанию корректен."),
        new("Invalid layout file: {0}. Built-in defaults will be used.", "布局文件无效：{0}。将使用内置默认值。", "レイアウトファイルが無効です: {0}。組み込み設定を使用します。", "Некорректный файл макета: {0}. Будет использован встроенный макет."),
        new("Open an inline detail panel before saving its layout.", "请先打开内嵌详细面板，再保存其布局。", "レイアウトを保存する前にインライン詳細パネルを開いてください。", "Перед сохранением макета откройте встроенную подробную панель."),
        new("Open an inline detail panel before loading its layout.", "请先打开内嵌详细面板，再加载其布局。", "レイアウトを読み込む前にインライン詳細パネルを開いてください。", "Перед загрузкой макета откройте встроенную подробную панель."),
        new("Saved inline detail layout: {0}", "已保存内嵌详细布局：{0}", "インライン詳細レイアウトを保存しました: {0}", "Встроенный подробный макет сохранен: {0}"),
        new("Loaded inline detail layout: {0}", "已加载内嵌详细布局：{0}", "インライン詳細レイアウトを読み込みました: {0}", "Встроенный подробный макет загружен: {0}"),
        new("Saved detail layout: {0}", "已保存详细布局：{0}", "詳細レイアウトを保存しました: {0}", "Подробный макет сохранен: {0}"),
        new("Loaded detail layout: {0}", "已加载详细布局：{0}", "詳細レイアウトを読み込みました: {0}", "Подробный макет загружен: {0}"),
        new("Layout save failed: {0}", "布局保存失败：{0}", "レイアウトの保存に失敗しました: {0}", "Ошибка сохранения макета: {0}"),
        new("Layout load failed: {0}", "布局加载失败：{0}", "レイアウトの読み込みに失敗しました: {0}", "Ошибка загрузки макета: {0}"),
        new("Ignores frequency bins unless both the focused source and comparison source are at or above this level.", "仅当当前信号源和对比信号源都达到或超过此电平时才分析该频点。", "対象ソースと比較ソースの両方がこのレベル以上の場合のみ周波数ビンを解析します。", "Частотные полосы учитываются, только если оба источника достигают этого уровня."),
        new("Maximum normalized level difference allowed between two prominent frequency bins.", "两个显著频点之间允许的最大归一化电平差。", "2 つの顕著な周波数ビン間で許容する正規化レベル差の上限です。", "Максимально допустимая нормализованная разница уровней двух выраженных частотных полос."),
        new("Requires both sources to rise above their nearby spectrum background before a frequency is treated as a collision.", "只有两个信号源都高于附近的频谱背景时，才将该频率视为冲突。", "両方のソースが周辺のスペクトル背景を上回った場合のみ、その周波数を衝突として扱います。", "Частота считается конфликтной, только если оба источника выделяются над соседним фоном спектра.")
    ];

    private static readonly Dictionary<string, TranslationEntry> EntriesBySource =
        Entries.ToDictionary(entry => entry.English, StringComparer.Ordinal);
    private static readonly ConditionalWeakTable<DependencyObject, Dictionary<DependencyProperty, string>> Originals = new();
    private static bool _initialized;

    public static event EventHandler? LanguageChanged;

    public static IReadOnlyList<LanguageOption> LanguageOptions { get; } =
    [
        new(AutomaticLanguageCode, "Automatic / 自动 / 自動 / Авто"),
        new(EnglishLanguageCode, "English"),
        new(ChineseLanguageCode, "简体中文"),
        new(JapaneseLanguageCode, "日本語"),
        new(RussianLanguageCode, "Русский")
    ];

    public static string PreferenceCode { get; private set; } = AutomaticLanguageCode;

    public static string CurrentLanguageCode { get; private set; } = EnglishLanguageCode;

    internal static IReadOnlyCollection<string> SourceTexts => EntriesBySource.Keys;

    public static void Initialize(string? preferenceCode)
    {
        if (!_initialized)
        {
            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnElementLoaded),
                handledEventsToo: true);
            _initialized = true;
        }

        Apply(preferenceCode);
    }

    public static void Apply(string? preferenceCode)
    {
        PreferenceCode = NormalizePreferenceCode(preferenceCode);
        CurrentLanguageCode = ResolveLanguageCode(PreferenceCode, CultureInfo.CurrentUICulture);

        if (Application.Current is { } application)
        {
            foreach (Window window in application.Windows)
            {
                LocalizeTree(window);
            }
        }

        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string Translate(string sourceText)
    {
        return Translate(sourceText, CurrentLanguageCode);
    }

    public static string Format(string sourceFormat, params object?[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, Translate(sourceFormat), arguments);
    }

    internal static string Translate(string sourceText, string languageCode)
    {
        if (!EntriesBySource.TryGetValue(sourceText, out var entry))
        {
            return sourceText;
        }

        return languageCode switch
        {
            ChineseLanguageCode => entry.Chinese,
            JapaneseLanguageCode => entry.Japanese,
            RussianLanguageCode => entry.Russian,
            _ => entry.English
        };
    }

    internal static IReadOnlyList<string> GetTranslations(string sourceText)
    {
        return EntriesBySource.TryGetValue(sourceText, out var entry)
            ? [entry.English, entry.Chinese, entry.Japanese, entry.Russian]
            : [];
    }

    public static string NormalizePreferenceCode(string? preferenceCode)
    {
        var match = LanguageOptions.FirstOrDefault(option =>
            string.Equals(option.Code, preferenceCode, StringComparison.OrdinalIgnoreCase));
        return match?.Code ?? AutomaticLanguageCode;
    }

    public static string ResolveLanguageCode(string? preferenceCode, CultureInfo systemCulture)
    {
        var normalized = NormalizePreferenceCode(preferenceCode);
        if (normalized != AutomaticLanguageCode)
        {
            return normalized;
        }

        var language = systemCulture.TwoLetterISOLanguageName;
        return language switch
        {
            "zh" => ChineseLanguageCode,
            "ja" => JapaneseLanguageCode,
            "ru" => RussianLanguageCode,
            _ => EnglishLanguageCode
        };
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject element)
        {
            LocalizeTree(element);
        }
    }

    internal static void LocalizeTree(DependencyObject root, string? languageCode = null)
    {
        LocalizeObject(root, languageCode);
        if (root is Popup { Child: DependencyObject popupChild })
        {
            LocalizeTree(popupChild, languageCode);
            return;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            LocalizeTree(VisualTreeHelper.GetChild(root, index), languageCode);
        }
    }

    private static void LocalizeObject(DependencyObject target, string? languageCode)
    {
        if (target is Window)
        {
            LocalizeProperty(target, Window.TitleProperty, languageCode);
        }

        if (target is TextBlock)
        {
            LocalizeProperty(target, TextBlock.TextProperty, languageCode);
        }

        if (target is ContentControl)
        {
            LocalizeProperty(target, ContentControl.ContentProperty, languageCode);
        }

        if (target is HeaderedContentControl)
        {
            LocalizeProperty(target, HeaderedContentControl.HeaderProperty, languageCode);
        }

        if (target is FrameworkElement)
        {
            LocalizeProperty(target, ToolTipService.ToolTipProperty, languageCode);
            LocalizeProperty(target, HoverHintBehavior.TextProperty, languageCode);
        }

        if (target is CorrelationMeter)
        {
            LocalizeProperty(target, CorrelationMeter.LabelProperty, languageCode);
        }

        if (target is ComboBox comboBox)
        {
            comboBox.Items.Refresh();
        }
    }

    private static void LocalizeProperty(
        DependencyObject target,
        DependencyProperty property,
        string? languageCode)
    {
        if (target.ReadLocalValue(property) is BindingExpressionBase)
        {
            return;
        }

        var originals = Originals.GetOrCreateValue(target);
        if (!originals.TryGetValue(property, out var sourceText))
        {
            if (target.GetValue(property) is not string currentText || !EntriesBySource.ContainsKey(currentText))
            {
                return;
            }

            sourceText = currentText;
            originals[property] = sourceText;
        }

        target.SetCurrentValue(property, Translate(sourceText, languageCode ?? CurrentLanguageCode));
    }

    private sealed record TranslationEntry(string English, string Chinese, string Japanese, string Russian);
}
