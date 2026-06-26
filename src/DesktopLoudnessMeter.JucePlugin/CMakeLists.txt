cmake_minimum_required(VERSION 3.22)

project(DESKTOP_LOUDNESS_METER_JUCE_PLUGIN VERSION 2.3.3 LANGUAGES CXX)

set(DLM_DEFAULT_JUCE_DIR "C:/JUCE")
if (DEFINED ENV{JUCE_DIR} AND NOT "$ENV{JUCE_DIR}" STREQUAL "")
    file(TO_CMAKE_PATH "$ENV{JUCE_DIR}" DLM_DEFAULT_JUCE_DIR)
elseif (DEFINED ENV{JUCE_ROOT} AND NOT "$ENV{JUCE_ROOT}" STREQUAL "")
    file(TO_CMAKE_PATH "$ENV{JUCE_ROOT}" DLM_DEFAULT_JUCE_DIR)
endif()

set(JUCE_DIR "${DLM_DEFAULT_JUCE_DIR}" CACHE PATH "Path to a JUCE checkout or install tree")
set(DLM_VST3_COPY_DIR "C:/Program Files/Common Files/VST3" CACHE PATH "Standard VST3 folder that receives the built plugin for DAW smoke tests")
set(DLM_EXTRA_VST3_COPY_DIR "C:/Program Files/VSTPlugins" CACHE PATH "Extra legacy scan folder that also receives the built VST3 bundle")
option(DLM_BUILD_JUCE_PLUGIN_TESTS "Build native JUCE plugin tests" ON)
option(DLM_COPY_VST3_AFTER_BUILD "Copy the built VST3 into DLM_VST3_COPY_DIR after each plugin build" OFF)
option(DLM_COPY_EXTRA_VST3_AFTER_BUILD "Also copy the built VST3 into DLM_EXTRA_VST3_COPY_DIR after each plugin build" OFF)

if (DLM_VST3_COPY_DIR STREQUAL "C:/Program Files/VSTPlugins")
    message(STATUS "Migrating DLM_VST3_COPY_DIR to the standard VST3 folder; VSTPlugins remains configured as DLM_EXTRA_VST3_COPY_DIR.")
    set(DLM_VST3_COPY_DIR "C:/Program Files/Common Files/VST3" CACHE PATH "Standard VST3 folder that receives the built plugin for DAW smoke tests" FORCE)
endif()

if (NOT EXISTS "${JUCE_DIR}/CMakeLists.txt")
    message(FATAL_ERROR "JUCE_DIR must point to a JUCE checkout containing CMakeLists.txt. Current value: ${JUCE_DIR}")
endif()

add_subdirectory("${JUCE_DIR}" "${CMAKE_BINARY_DIR}/JUCE" EXCLUDE_FROM_ALL)

juce_add_plugin(DesktopLoudnessMeterSender
    COMPANY_NAME "Campfireman Studio"
    BUNDLE_ID com.campfireman.umeter.sender
    IS_SYNTH FALSE
    NEEDS_MIDI_INPUT FALSE
    NEEDS_MIDI_OUTPUT FALSE
    IS_MIDI_EFFECT FALSE
    EDITOR_WANTS_KEYBOARD_FOCUS TRUE
    COPY_PLUGIN_AFTER_BUILD ${DLM_COPY_VST3_AFTER_BUILD}
    VST3_COPY_DIR "${DLM_VST3_COPY_DIR}"
    PLUGIN_MANUFACTURER_CODE DlmT
    PLUGIN_CODE Dls1
    FORMATS VST3
    PRODUCT_NAME "uMeter Sender")

target_sources(DesktopLoudnessMeterSender
    PRIVATE
        Source/LoopbackProtocol.cpp
        Source/LoopbackSender.cpp
        Source/PluginEditor.cpp
        Source/PluginState.cpp
        Source/PluginProcessor.cpp
        Source/RealtimeAudioQueue.cpp)

target_compile_features(DesktopLoudnessMeterSender PRIVATE cxx_std_17)

if (MSVC)
    target_compile_options(DesktopLoudnessMeterSender PRIVATE /utf-8)
    set_target_properties(DesktopLoudnessMeterSender PROPERTIES
        VS_GLOBAL_VcpkgEnabled false
        VS_GLOBAL_VcpkgApplocalDeps false)
endif()

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

if (MSVC)
    foreach (dlm_generated_target
        DesktopLoudnessMeterSender_VST3
        DesktopLoudnessMeterSender_vst3_helper)
        if (TARGET ${dlm_generated_target})
            target_compile_options(${dlm_generated_target} PRIVATE /utf-8)
            set_target_properties(${dlm_generated_target} PROPERTIES
                VS_GLOBAL_VcpkgEnabled false
                VS_GLOBAL_VcpkgApplocalDeps false)
        endif()
    endforeach()
endif()

if (DLM_COPY_EXTRA_VST3_AFTER_BUILD)
    add_custom_command(TARGET DesktopLoudnessMeterSender_VST3 POST_BUILD
        COMMAND "${CMAKE_COMMAND}" -E make_directory "${DLM_EXTRA_VST3_COPY_DIR}"
        COMMAND "${CMAKE_COMMAND}" -E remove_directory "${DLM_EXTRA_VST3_COPY_DIR}/uMeter Sender.vst3"
        COMMAND "${CMAKE_COMMAND}" -E remove_directory "${DLM_EXTRA_VST3_COPY_DIR}/Desktop Loudness Meter Sender.vst3"
        COMMAND "${CMAKE_COMMAND}" -E copy_directory
            "$<TARGET_FILE_DIR:DesktopLoudnessMeterSender_VST3>/../.."
            "${DLM_EXTRA_VST3_COPY_DIR}/uMeter Sender.vst3"
        VERBATIM)
endif()

if (DLM_BUILD_JUCE_PLUGIN_TESTS)
    enable_testing()
    add_subdirectory(Tests)
endif()
