cmake_minimum_required(VERSION 3.22)

project(DESKTOP_LOUDNESS_METER_JUCE_AAX_PLUGIN VERSION 2.2.7 LANGUAGES CXX)

set(JUCE_DIR "C:/JUCE" CACHE PATH "Path to a JUCE checkout or install tree")
set(DLM_AAX_SDK_DIR "" CACHE PATH "Optional path to an AAX SDK checkout. Leave empty to use JUCE's bundled AAX SDK path when available.")
set(DLM_AAX_COPY_DIR "" CACHE PATH "Optional folder that receives the built AAX bundle when copy-after-build is enabled")
option(DLM_COPY_AAX_AFTER_BUILD "Copy the built AAX bundle into DLM_AAX_COPY_DIR after each plugin build" OFF)

if (NOT EXISTS "${JUCE_DIR}/CMakeLists.txt")
    message(FATAL_ERROR "JUCE_DIR must point to a JUCE checkout containing CMakeLists.txt. Current value: ${JUCE_DIR}")
endif()

add_subdirectory("${JUCE_DIR}" "${CMAKE_BINARY_DIR}/JUCE" EXCLUDE_FROM_ALL)

if (NOT DLM_AAX_SDK_DIR STREQUAL "")
    if (NOT EXISTS "${DLM_AAX_SDK_DIR}/Interfaces/ACF")
        message(FATAL_ERROR "DLM_AAX_SDK_DIR does not look like an AAX SDK checkout: ${DLM_AAX_SDK_DIR}")
    endif()

    juce_set_aax_sdk_path("${DLM_AAX_SDK_DIR}")
endif()

set(DLM_JUCE_SENDER_SOURCE_DIR "${CMAKE_CURRENT_LIST_DIR}/../DesktopLoudnessMeter.JucePlugin/Source")

juce_add_plugin(DesktopLoudnessMeterSender
    COMPANY_NAME "Campfireman Studio"
    BUNDLE_ID com.campfireman.umeter.sender
    IS_SYNTH FALSE
    NEEDS_MIDI_INPUT FALSE
    NEEDS_MIDI_OUTPUT FALSE
    IS_MIDI_EFFECT FALSE
    EDITOR_WANTS_KEYBOARD_FOCUS TRUE
    AAX_CATEGORY Effect
    COPY_PLUGIN_AFTER_BUILD ${DLM_COPY_AAX_AFTER_BUILD}
    AAX_COPY_DIR "${DLM_AAX_COPY_DIR}"
    PLUGIN_MANUFACTURER_CODE DlmT
    PLUGIN_CODE Dls1
    FORMATS AAX
    PRODUCT_NAME "uMeter Sender")

target_sources(DesktopLoudnessMeterSender
    PRIVATE
        "${DLM_JUCE_SENDER_SOURCE_DIR}/LoopbackProtocol.cpp"
        "${DLM_JUCE_SENDER_SOURCE_DIR}/LoopbackSender.cpp"
        "${DLM_JUCE_SENDER_SOURCE_DIR}/PluginEditor.cpp"
        "${DLM_JUCE_SENDER_SOURCE_DIR}/PluginState.cpp"
        "${DLM_JUCE_SENDER_SOURCE_DIR}/PluginProcessor.cpp"
        "${DLM_JUCE_SENDER_SOURCE_DIR}/RealtimeAudioQueue.cpp")

target_include_directories(DesktopLoudnessMeterSender
    PRIVATE
        "${DLM_JUCE_SENDER_SOURCE_DIR}")

target_compile_features(DesktopLoudnessMeterSender PRIVATE cxx_std_17)

target_compile_definitions(DesktopLoudnessMeterSender
    PUBLIC
        JUCE_WEB_BROWSER=0
        JUCE_USE_CURL=0
        JUCE_VST3_CAN_REPLACE_VST2=0)

target_link_libraries(DesktopLoudnessMeterSender
    PRIVATE
        juce::juce_audio_utils
    PUBLIC
        juce::juce_recommended_config_flags
        juce::juce_recommended_lto_flags
        juce::juce_recommended_warning_flags)
