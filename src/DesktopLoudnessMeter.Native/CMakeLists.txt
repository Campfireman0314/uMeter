cmake_minimum_required(VERSION 3.20)

project(DesktopLoudnessMeterNative LANGUAGES CXX)

add_library(DesktopLoudnessMeter.Native SHARED
    DesktopLoudnessMeterNative.cpp
    DlmHostAggregateSnapshot.cpp
    DlmHostCollisionProjection.cpp
    DlmHostChartSettingsCommand.cpp
    DlmHostDetailModuleProjection.cpp
    DlmHostGlobalSettingsCommand.cpp
    DlmHostRecentEvents.cpp
    DlmHostRuntimeState.cpp
    DlmHostRuntimeServiceCommand.cpp
    DlmHostSelectedDetailSnapshot.cpp
    DlmHostSelectedDetailShell.cpp
    DlmHostSelectedSourceCommand.cpp
    DlmHostSnapshotChromeProjection.cpp
    DlmHostSourceOrderCommand.cpp
    DlmHostSourcePreferenceCommand.cpp
    DlmHostSourceProjection.cpp
    DlmHostSourceTargetCommand.cpp
    DlmLoopbackProtocol.cpp
    DlmPcmAnalyzer.Core.cpp
    DlmPcmAnalyzer.Loudness.cpp
    DlmPcmAnalyzer.RecentWindow.cpp
    DlmPcmAnalyzer.TruePeak.cpp
    DlmSourceIngestCore.cpp
    RfRapid/RfCPUID.cpp
    RfRapid/RfRapid.cpp
)

target_compile_features(DesktopLoudnessMeter.Native PRIVATE cxx_std_17)

target_include_directories(DesktopLoudnessMeter.Native
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/RfRapid
)

target_compile_definitions(DesktopLoudnessMeter.Native
    PRIVATE
        EXPORT
        _CRT_SECURE_NO_WARNINGS
)

if(WIN32)
    target_compile_definitions(DesktopLoudnessMeter.Native PRIVATE WIN32 _WINDOWS)
    set_target_properties(DesktopLoudnessMeter.Native PROPERTIES
        OUTPUT_NAME "DesktopLoudnessMeter.Native"
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/x64/$<CONFIG>"
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/x64/$<CONFIG>"
        ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/obj/x64/$<CONFIG>"
    )

    if(MSVC)
        target_compile_options(DesktopLoudnessMeter.Native PRIVATE /utf-8 /arch:AVX2)
    endif()
else()
    set_target_properties(DesktopLoudnessMeter.Native PROPERTIES
        OUTPUT_NAME "DesktopLoudnessMeter.Native"
        CXX_VISIBILITY_PRESET hidden
        VISIBILITY_INLINES_HIDDEN YES
    )
endif()
