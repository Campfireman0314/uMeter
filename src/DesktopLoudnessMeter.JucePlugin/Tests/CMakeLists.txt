juce_add_console_app(DesktopLoudnessMeterJucePluginTests
    PRODUCT_NAME "uMeter JUCE Plugin Tests")

target_sources(DesktopLoudnessMeterJucePluginTests
    PRIVATE
        ../Source/LoopbackProtocol.cpp
        ../Source/LoopbackSender.cpp
        ../Source/PluginState.cpp
        ../Source/RealtimeAudioQueue.cpp
        TestMain.cpp
        LoopbackSenderTests.cpp
        LoopbackProtocolTests.cpp
        PluginStateTests.cpp
        RealtimeAudioQueueTests.cpp)

target_include_directories(DesktopLoudnessMeterJucePluginTests
    PRIVATE
        "${CMAKE_CURRENT_LIST_DIR}/../Source")

target_compile_features(DesktopLoudnessMeterJucePluginTests PRIVATE cxx_std_17)

if (MSVC)
    target_compile_options(DesktopLoudnessMeterJucePluginTests PRIVATE /utf-8)
    set_target_properties(DesktopLoudnessMeterJucePluginTests PROPERTIES
        VS_GLOBAL_VcpkgEnabled false
        VS_GLOBAL_VcpkgApplocalDeps false)
endif()

target_compile_definitions(DesktopLoudnessMeterJucePluginTests
    PUBLIC
        JUCE_WEB_BROWSER=0
        JUCE_USE_CURL=0)

target_link_libraries(DesktopLoudnessMeterJucePluginTests
    PRIVATE
        juce::juce_audio_basics
    PUBLIC
        juce::juce_recommended_config_flags
        juce::juce_recommended_warning_flags)

add_test(NAME DesktopLoudnessMeterJucePluginTests COMMAND DesktopLoudnessMeterJucePluginTests)
