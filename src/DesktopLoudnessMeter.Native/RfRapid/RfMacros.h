 #pragma once

// Multi platform compiler macros.

#if defined(_WIN32) || defined(__CYGWIN__)
#define RF_CALL __cdecl
#else
#define RF_CALL
#endif

// ------------------------------------------------------
// Debug macros
#ifdef _DEBUG
#define RF_DBG(_content_) std::cout << std::string(_content_) << std::endl
#else
#define RF_DBG(_content_) 
#endif

// ------------------------------------------------------
// DLL export/import descriptions

#ifndef PLATFORM_DYN_LIB_EXPORT_HEADER

#ifdef _DEV
	// Code for other platforms
#define LINE_FEED_SIZE 2
#define PLATFORM_DYN_LIB_EXPORT_HEADER 

#elif defined(__ANDROID__) || defined(ANDROID)
	// Android specific code
#define LINE_FEED_SIZE 1
#define PLATFORM_DYN_LIB_EXPORT_HEADER __attribute__((visibility("default"))) __attribute__((used))
#define RF_MOBILE

#elif defined(__APPLE__)
	// iOS specific code
#define LINE_FEED_SIZE 1
#define PLATFORM_DYN_LIB_EXPORT_HEADER __attribute__((visibility("default"))) __attribute__((used))
#define RF_MOBILE

#elif defined(_WIN32)
	// Windows specific code
#define LINE_FEED_SIZE 2
#ifdef EXPORT
#define PLATFORM_DYN_LIB_EXPORT_HEADER __declspec(dllexport)
#else
#define PLATFORM_DYN_LIB_EXPORT_HEADER __declspec(dllimport)
#endif

#elif defined(LINUX) || defined(__LINUX__)
	// Other platforms
#define PLATFORM_DYN_LIB_EXPORT_HEADER
#define LINE_FEED_SIZE 1

#endif

#endif

// ------------------------------------------------------
// Instruction set switches
#if defined(_M_X86) || defined(__i386__)
#define RF_X86
#define RF_X86_32
#elif defined(_M_X64) || defined(__x86_64__)
#define RF_X86
#define RF_X86_64
#elif defined(_M_ARM64) || defined(__aarch64__)
#define RF_ARM
#define RF_ARM64
#elif defined(_M_ARM) || defined(__arm__)
#define RF_ARM
#define RF_ARM32
#endif

// ------------------------------------------------------
// DLL load/release operations
#if defined(__ANDROID__)
// Android specific code

#include <dlfcn.h> 

#define LoadDLL(_out_dll_handle_, _dll_path_)\
	void* _out_dll_handle_ = dlopen(_dll_path_, RTLD_NOW);\
	if(!_out_dll_handle_) std::cout << #_out_dll_handle_ << " failed to load. " << std::endl;

#define GetDLLFunction(_dll_handle, _name_)\
	dlsym(_dll_handle, _name_);

#define UnloadDLL(_dll_handle)\
	dlclose(_dll_handle);

#elif defined(__APPLE__) && (TARGET_OS_IOS || TARGET_OS_IPHONE)
// iOS specific code

#include <dlfcn.h> 

#define LoadDLL(_out_dll_handle_, _dll_path_)\
	void* _out_dll_handle_ = dlopen(_dll_path_, RTLD_LOCAL);\
	if(!_out_dll_handle_) std::cout << #_out_dll_handle_ << " failed to load. " << std::endl;

#define GetDLLFunction(_dll_handle, _name_)\
	dlsym(_dll_handle, _name_);

#define UnloadDLL(_dll_handle)\
	dlclose(_dll_handle);

#elif defined(_WIN32)
// Windows specific code

#include <windows.h>

#ifdef UNICODE


#define LoadDLL(_out_dll_handle_, _dll_path_)\
	HINSTANCE _out_dll_handle_ = LoadLibrary(L#_dll_path_);\
	if(!_out_dll_handle_) std::cout << #_out_dll_handle_ << " failed to load. " << std::endl;

#else

#define LoadDLL(_out_dll_handle_, _dll_path_)\
	HINSTANCE _out_dll_handle_ = LoadLibrary(_dll_path_);\
	if(!_out_dll_handle_) std::cout << #_out_dll_handle_ << " failed to load. " << std::endl;

#endif


//#define LoadDLL(_out_dll_handle_, _dll_path_)\
//	HINSTANCE _out_dll_handle_ = LoadLibraryA(_dll_path_);\
//	if(!_out_dll_handle_) std::cout << #_out_dll_handle_ " failed to load. " << std::endl

#define GetDLLFunction(_dll_handle, _name_)\
	GetProcAddress((HINSTANCE)_dll_handle, _name_)

#define UnloadDLL(_dll_handle)\
	FreeLibrary((HINSTANCE)_dll_handle)

#endif

#define RfClass class PLATFORM_DYN_LIB_EXPORT_HEADER
#define RfExportFunc extern "C" PLATFORM_DYN_LIB_EXPORT_HEADER

extern bool if_log;

// Ak Debug macros
#if _WIN32

#include <filesystem>
#include <fstream>
#include <iostream>
#include <shlobj.h>

#define DBG(_msg) \
{\
	if (if_log)\
	{\
		wchar_t path[MAX_PATH];\
		SHGetSpecialFolderPath(0, path, CSIDL_DESKTOPDIRECTORY, 0);\
		DWORD dwNum = WideCharToMultiByte(CP_OEMCP, NULL, path, -1, NULL, 0, NULL, FALSE);\
		char* psText = new char[dwNum];\
		WideCharToMultiByte(CP_OEMCP, NULL, path, -1, psText, dwNum, NULL, FALSE);\
		std::string rst = psText;\
		delete[] psText;\
		std::ofstream fout;\
		fout.open(rst + "\\GradientVerbEffectDllLog.txt", std::ios::out | std::ios::app);\
		fout << "[RF] " << _msg << std::endl;\
		fout.close();\
	}\
}

#elif __ANDROID__
#include <android/log.h>
#include <sstream>

#define DBG(_msg) \
    {\
        if (if_log)\
        {\
            std::stringstream ss; \
            ss << _msg; \
            __android_log_print(ANDROID_LOG_INFO, "GradientVerb", "[RF] %s", ss.str().c_str()); \
        }\
    }

#else
#include <iostream>
#define DBG(_msg) \
    {\
        if (if_log)\
        {\
            std::cout << "[RF] " << _msg << std::endl; \
        }\
    }

#endif
