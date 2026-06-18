/////////////////////////////////////////////////////////////////////
// 
// Numeral type capsule.
//
/////////////////////////////////////////////////////////////////////

#ifndef _RF_NUMERAL_TYPES_H_
#define _RF_NUMERAL_TYPES_H_

#include <stdint.h>
#include <float.h>
#include <limits>
#include <climits>
#include <type_traits>

// ============================================
// Numeral types
typedef uint8_t		RfUInt8;
typedef uint16_t	RfUInt16;
typedef uint32_t	RfUInt32;
typedef uint64_t	RfUInt64;

typedef int8_t		RfInt8;
typedef int16_t		RfInt16;
typedef int32_t		RfInt32;
typedef int64_t		RfInt64;

typedef float		RfReal32;
typedef double		RfReal64;

typedef bool		RfBool;

typedef intptr_t	RfIntPtr;
typedef uintptr_t	AkUIntPtr;

#define dsp_type_assert(_DATA_TYPE_) \
static_assert(std::is_same_v<RfReal32, _DATA_TYPE_> || std::is_same_v<RfReal64, _DATA_TYPE_>, "Data must be either float or double.")


// ============================================
// Complex number

template <typename T>
struct RfCplx {
    dsp_type_assert(T);

    T re;
    T im;

    RfCplx() : re(0.0f), im(0.0f) {}
    RfCplx(T r, T i = 0.0f) : re(r), im(i) {}
    
    // Complex arithmetic
    RfCplx operator+(const RfCplx& other) const {
        return RfCplx(re + other.re, im + other.im);
    }
    
    RfCplx operator-(const RfCplx& other) const {
        return RfCplx(re - other.re, im - other.im);
    }
    
    RfCplx operator*(const RfCplx& other) const {
        return RfCplx(
            re * other.re - im * other.im,
            re * other.im + im * other.re
        );
    }

    RfCplx operator/(const RfCplx& other) const {
        T denom = other.re * other.re + other.im * other.im;
        return RfCplx(
            (re * other.re + im * other.im) / denom,
            (im * other.re - re * other.im) / denom
        );
    }
    
    RfCplx& operator+=(const RfCplx& other) {
        re += other.re;
        im += other.im;
        return *this;
    }

    RfCplx& operator-=(const RfCplx& other) {
        re -= other.re;
        im -= other.im;
        return *this;
    }
    
    RfCplx& operator*=(const RfCplx& other) {
        T new_re = re * other.re - im * other.im;
        im = re * other.im + im * other.re;
        re = new_re;
        return *this;
    }

    RfCplx& operator/=(const RfCplx& other) {
        T denom = other.re * other.re + other.im * other.im;
        re = (re * other.re + im * other.im) / denom;
        im = (im * other.re - re * other.im) / denom;
        return *this;
    }
};

typedef struct alignas(8) RfCplx32 : RfCplx<RfReal32> {
    RfCplx32() : RfCplx<RfReal32>() {}
    RfCplx32(RfReal32 r, RfReal32 i = 0.0f) : RfCplx<RfReal32>(r, i) {}
} RfCplx32;
typedef struct alignas(16) RfCplx64 : RfCplx<RfReal64> {
    RfCplx64() : RfCplx<RfReal64>() {}
    RfCplx64(RfReal64 r, RfReal64 i = 0.0) : RfCplx<RfReal64>(r, i) {}
} RfCplx64;
#undef float_type_assert


// ============================================
// Limits

#define RfUInt8_Max			(UCHAR_MAX)
#define RfUInt16_Max		(USHRT_MAX)
#define RfUInt32_Max		(UINT_MAX)
#define RfUInt64_Max		(ULLONG_MAX)

#define RfInt8_Min			(SCHAR_MIN)
#define RfInt8_Max			(SCHAR_MAX)
#define RfInt16_Min			(SHRT_MIN)
#define RfInt16_Max			(SHRT_MAX)
#define RfInt32_Min			(INT_MIN)
#define RfInt32_Max			(INT_MAX)
#define RfInt64_Min			(LLONG_MIN)
#define RfInt64_Max			(LLONG_MAX)

#define RfReal32_Epsilon	(FLT_EPSILON)
#define RfReal64_Epsilon	(DBL_EPSILON)

#define RfReal32_Min		(-FLT_MAX)
#define RfReal32_Max		(FLT_MAX)
#define RfReal64_Min		(-DBL_MAX)
#define RfReal64_Max		(DBL_MAX)

#endif