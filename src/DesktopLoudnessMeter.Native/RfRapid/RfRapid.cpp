#include "RfRapid.h"
// #include "../common/RfMaths.h"
#include <cmath>
#include <mutex>

#ifdef _RF_RAPID_

#if defined(RF_SSE)
#include <emmintrin.h>
#include <pmmintrin.h>  // SSE3 for _mm_addsub_ps/pd
#include <xmmintrin.h>  // For _MM_SET_FLUSH_ZERO_MODE
#if defined(RF_DETECT_AVX2)
#include <immintrin.h>
#endif
#elif defined(RF_NEON)
#include <arm_neon.h>
#endif

// =============================================================================
// Denormal handling utilities
// =============================================================================

namespace DenormalGuard {

/**
 * @brief Enable Flush-To-Zero (FTZ) and Denormals-Are-Zero (DAZ) modes
 * 
 * FTZ: Results that would be denormal are flushed to zero
 * DAZ: Denormal inputs are treated as zero
 * 
 * This prevents the massive performance penalty (up to 100x slower)
 * that occurs when the CPU processes denormal floating-point numbers.
 * 
 * Critical for real-time audio processing where denormals can cause
 * audio dropouts and glitches.
 */
inline void enableFlushToZero() {
#if defined(RF_SSE) || defined(__SSE__)
    // SSE: Set FTZ (Flush-To-Zero) mode
    _MM_SET_FLUSH_ZERO_MODE(_MM_FLUSH_ZERO_ON);
    
    // SSE3+: Set DAZ (Denormals-Are-Zero) mode
    // DAZ requires SSE3 or later, but _mm_setcsr is available in SSE
    #if defined(__SSE3__) || defined(RF_SSE)
    // Set both FTZ and DAZ bits in MXCSR register
    // Bit 15 = FTZ, Bit 6 = DAZ
    unsigned int mxcsr = _mm_getcsr();
    mxcsr |= (1 << 15);  // FTZ bit
    mxcsr |= (1 << 6);   // DAZ bit (requires CPU support)
    _mm_setcsr(mxcsr);
    #endif
    
#elif defined(RF_NEON) && defined(__ARM_NEON)
    // ARM NEON: Set FZ (Flush-to-Zero) bit in FPCR
    #if defined(__aarch64__)
    // AArch64: Use FPCR register
    uint64_t fpcr;
    __asm__ __volatile__("mrs %0, fpcr" : "=r"(fpcr));
    fpcr |= (1 << 24);  // FZ bit
    __asm__ __volatile__("msr fpcr, %0" : : "r"(fpcr));
    #else
    // ARMv7: Use FPSCR register  
    uint32_t fpscr;
    __asm__ __volatile__("vmrs %0, fpscr" : "=r"(fpscr));
    fpscr |= (1 << 24);  // FZ bit
    __asm__ __volatile__("vmsr fpscr, %0" : : "r"(fpscr));
    #endif
#endif
}

/**
 * @brief Check if FTZ/DAZ modes are currently enabled
 * @return true if denormal handling is active
 */
inline bool isFlushToZeroEnabled() {
#if defined(RF_SSE) || defined(__SSE__)
    unsigned int mxcsr = _mm_getcsr();
    bool ftz = (mxcsr & (1 << 15)) != 0;
    bool daz = (mxcsr & (1 << 6)) != 0;
    return ftz && daz;
#elif defined(RF_NEON) && defined(__aarch64__)
    uint64_t fpcr;
    __asm__ __volatile__("mrs %0, fpcr" : "=r"(fpcr));
    return (fpcr & (1 << 24)) != 0;
#else
    return false;
#endif
}

} // namespace DenormalGuard

using namespace RF;

// ================================================================================
// Common helpers
#define TWOPI (6.28318530717958647692)




namespace Helpers
{
	template <typename T>
	void inline cfft_bit_reverse(T*& data, size_t n) 
	{
		// Compute number of bits
		int bits = 0;
		size_t temp = n - 1;
		while (temp) {
			++bits;
			temp >>= 1;
		}

		// Perform bit-reversal permutation
		for (size_t i = 0; i < n; ++i) {
			size_t j = 0;
			for (int k = 0; k < bits; ++k) {
				j = (j << 1) | ((i >> k) & 1);
			}
			if (i < j) {
				std::swap(data[i], data[j]);
			}
		}
	}

	// void inline cfft_bit_reverse(RfCplx64*& data, size_t n)
	// {
	// 	int bits = 0;
	// 	size_t temp = n - 1;
	// 	while (temp) {
	// 		++bits;
	// 		temp >>= 1;
	// 	}

	// 	for (size_t i = 0; i < n; ++i) {
	// 		size_t j = 0;
	// 		for (int k = 0; k < bits; ++k) {
	// 			j = (j << 1) | ((i >> k) & 1);
	// 		}
	// 		if (i < j) {
	// 			std::swap(data[i], data[j]);
	// 		}
	// 	}
	// }
}


// ================================================================================
// Fast vector operators


// ------------------------------------------------------
// Basic vector operator
void vec_op_default::fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ + *in_vec2++;
		--size;
	}
}

void vec_op_default::fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ * *in_vec2++;
		--size;
	}
}

void vec_op_default::fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ + add;
		--size;
	}
}

void vec_op_default::fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ * mul;
		--size;
	}
}

void vec_op_default::fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ - *in_vec2++;
		--size;
	}
}

void vec_op_default::fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ - sub;
		--size;
	}
}

void vec_op_default::fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ / *in_vec2++;
		--size;
	}
}

void vec_op_default::fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ / div;
		--size;
	}
}

void vec_op_default::fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	*out = 0;
	while (size) {
		*out += *in_vec++;
		--size;
	}
}

void vec_op_default::fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	*out = 0;
	union {
		RfReal32 f;
		RfUInt32 i;
	} u{};
	while (size) {
		u.f = *in_vec++;
		u.i &= 0x7fffffff;
		*out += u.f;
		--size;
	}
}

void vec_op_default::fvec_copy(const RfReal32* src, RfReal32* dst, size_t size)
{
	while (size) {
		*dst++ = *src++;
		--size;
	}
}

void vec_op_default::fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size)
{
	RfReal32 val = *value;
	while (size) {
		*dst++ = val;
		--size;
	}
}

void vec_op_default::dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ + *in_vec2++;
		--size;
	}
}

void vec_op_default::dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ * *in_vec2++;
		--size;
	}
}

void vec_op_default::dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ + add;
		--size;
	}
}

void vec_op_default::dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size)
{
	while (size) {
		*out_vec++ = *in_vec1++ * mul;
		--size;
	}
}

void vec_op_default::dvec_copy(const RfReal64* src, RfReal64* dst, size_t size)
{
	while (size) {
		*dst++ = *src++;
		--size;
	}
}

void vec_op_default::cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size)
{
	while (size) {
		out_vec->re = in_vec1->re * in_vec2->re - in_vec1->im * in_vec2->im;
		out_vec->im = in_vec1->re * in_vec2->im + in_vec1->im * in_vec2->re;
		out_vec++;
		in_vec1++;
		in_vec2++;
		size--;
	}
}

void vec_op_default::cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size)
{
	while (size) {
		out_vec->re = in_vec1->re * in_vec2->re - in_vec1->im * in_vec2->im;
		out_vec->im = in_vec1->re * in_vec2->im + in_vec1->im * in_vec2->re;
		out_vec++;
		in_vec1++;
		in_vec2++;
		size--;
	}
}

void vec_op_default::cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size)
{
	while (size) {
		out_vec->re += in_vec1->re * in_vec2->re - in_vec1->im * in_vec2->im;
		out_vec->im += in_vec1->re * in_vec2->im + in_vec1->im * in_vec2->re;
		out_vec++;
		in_vec1++;
		in_vec2++;
		size--;
	}
}

void vec_op_default::cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size)
{
	while (size) {
		out_vec->re += in_vec1->re * in_vec2->re - in_vec1->im * in_vec2->im;
		out_vec->im += in_vec1->re * in_vec2->im + in_vec1->im * in_vec2->re;
		out_vec++;
		in_vec1++;
		in_vec2++;
		size--;
	}
}

// Convolution dot product operations
RfReal32 vec_op_default::fconv_dot_product(const RfReal32* a, const RfReal32* b, size_t size)
{
	RfReal32 acc = 0.0f;
	for (size_t i = 0; i < size; ++i) {
		acc += a[i] * b[i];
	}
	return acc;
}

RfReal64 vec_op_default::dconv_dot_product(const RfReal64* a, const RfReal64* b, size_t size)
{
	RfReal64 acc = 0.0;
	for (size_t i = 0; i < size; ++i) {
		acc += a[i] * b[i];
	}
	return acc;
}

void vec_op_default::fconv_direct_kernel(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count)
{
	for (RfInt32 i = 0; i < count; ++i) {
		RfReal32 acc = 0.0f;
		for (RfInt32 j = 0; j < kernel_len; ++j) {
			RfInt32 read_pos = (write_pos - 1 - j + hist_size) & hist_mask;
			acc += kernel[j] * history[read_pos];
		}
		output[i] = acc;
		write_pos++;
	}
}

void vec_op_default::dconv_direct_kernel(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count)
{
	for (RfInt32 i = 0; i < count; ++i) {
		RfReal64 acc = 0.0;
		for (RfInt32 j = 0; j < kernel_len; ++j) {
			RfInt32 read_pos = (write_pos - 1 - j + hist_size) & hist_mask;
			acc += kernel[j] * history[read_pos];
		}
		output[i] = acc;
		write_pos++;
	}
}

void vec_op_default::fconv_partition(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count, RfInt32 partition_size)
{
	for (RfInt32 i = 0; i < count; ++i) {
		RfReal32 acc = 0.0f;
		for (RfInt32 k = 0; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			acc += kernel[k] * history[read_pos];
		}
		output[i] = acc;
		write_pos++;
	}
}

void vec_op_default::dconv_partition(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count, RfInt32 partition_size)
{
	for (RfInt32 i = 0; i < count; ++i) {
		RfReal64 acc = 0.0;
		for (RfInt32 k = 0; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			acc += kernel[k] * history[read_pos];
		}
		output[i] = acc;
		write_pos++;
	}
}

// ============================================================================
// FFT-specific operations (default scalar implementations)
// ============================================================================

void vec_op_default::cfft_butterfly(RfCplx32* data, size_t n, size_t stride)
{
	// Butterfly operation: for pairs (a,b), compute (a+b, a-b)
	for (size_t i = 0; i < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		RfReal32 re_a = data[idx1].re;
		RfReal32 im_a = data[idx1].im;
		RfReal32 re_b = data[idx2].re;
		RfReal32 im_b = data[idx2].im;

		data[idx1].re = re_a + re_b;
		data[idx1].im = im_a + im_b;
		data[idx2].re = re_a - re_b;
		data[idx2].im = im_a - im_b;
	}
}

void vec_op_default::cfft_butterfly(RfCplx64* data, size_t n, size_t stride)
{
	for (size_t i = 0; i < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		RfReal64 re_a = data[idx1].re;
		RfReal64 im_a = data[idx1].im;
		RfReal64 re_b = data[idx2].re;
		RfReal64 im_b = data[idx2].im;

		data[idx1].re = re_a + re_b;
		data[idx1].im = im_a + im_b;
		data[idx2].re = re_a - re_b;
		data[idx2].im = im_a - im_b;
	}
}

void vec_op_default::cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle)
{
	// Multiply every other element by twiddle factor
	for (size_t i = 0; i < n; i += 2) {
		RfReal32 re = data[i].re * twiddle->re - data[i].im * twiddle->im;
		RfReal32 im = data[i].re * twiddle->im + data[i].im * twiddle->re;
		data[i].re = re;
		data[i].im = im;
	}
}

void vec_op_default::cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle)
{
	for (size_t i = 0; i < n; i += 2) {
		RfReal64 re = data[i].re * twiddle->re - data[i].im * twiddle->im;
		RfReal64 im = data[i].re * twiddle->im + data[i].im * twiddle->re;
		data[i].re = re;
		data[i].im = im;
	}
}

void vec_op_default::cfft_bit_reverse(RfCplx32* data, size_t n)
{
	Helpers::cfft_bit_reverse(data, n);
}

void vec_op_default::cfft_bit_reverse(RfCplx64* data, size_t n)
{
	Helpers::cfft_bit_reverse(data, n);
}

void vec_op_default::cfft_scale(RfCplx32* data, size_t n, RfReal32 scale)
{
	for (size_t i = 0; i < n; ++i) {
		data[i].re *= scale;
		data[i].im *= scale;
	}
}

void vec_op_default::cfft_scale(RfCplx64* data, size_t n, RfReal64 scale)
{
	for (size_t i = 0; i < n; ++i) {
		data[i].re *= scale;
		data[i].im *= scale;
	}
}

// ------------------------------------------------------
// SSE vector operator
#ifdef RF_SSE

// ------------------------------------------------------
// SSE FFT operations
void vec_op_sse::cfft_butterfly(RfCplx32* data, size_t n, size_t stride)
{
	// Process 2 complex pairs (4 complex numbers) at a time
	for (size_t i = 0; i + 3 * stride < n; i += 4 * stride) {
		size_t idx0 = i;
		size_t idx1 = i + stride;
		size_t idx2 = i + 2 * stride;
		size_t idx3 = i + 3 * stride;

		__m128 a = _mm_loadu_ps((const float*)&data[idx0]);
		__m128 b = _mm_loadu_ps((const float*)&data[idx2]);

		// Butterfly: (a+b, a-b) using SIMD add/sub
		__m128 sum = _mm_add_ps(a, b);
		__m128 diff = _mm_sub_ps(a, b);

		_mm_storeu_ps((float*)&data[idx0], sum);
		_mm_storeu_ps((float*)&data[idx2], diff);
	}

	// Handle remaining elements
	for (size_t i = (n / (4 * stride)) * (4 * stride); i + stride < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		RfReal32 re_a = data[idx1].re;
		RfReal32 im_a = data[idx1].im;
		RfReal32 re_b = data[idx2].re;
		RfReal32 im_b = data[idx2].im;

		data[idx1].re = re_a + re_b;
		data[idx1].im = im_a + im_b;
		data[idx2].re = re_a - re_b;
		data[idx2].im = im_a - im_b;
	}
}

void vec_op_sse::cfft_butterfly(RfCplx64* data, size_t n, size_t stride)
{
	for (size_t i = 0; i + stride < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		__m128d a = _mm_loadu_pd((const double*)&data[idx1]);
		__m128d b = _mm_loadu_pd((const double*)&data[idx2]);

		__m128d sum = _mm_add_pd(a, b);
		__m128d diff = _mm_sub_pd(a, b);

		_mm_storeu_pd((double*)&data[idx1], sum);
		_mm_storeu_pd((double*)&data[idx2], diff);
	}
}

void vec_op_sse::cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle)
{
	const static int shuffle = _MM_SHUFFLE(2, 3, 0, 1);
	const static __m128 mask = _mm_castsi128_ps(_mm_set_epi32(0, RfUInt32_Max, 0, RfUInt32_Max));

	size_t i = 0;
	for (; i + 2 < n; i += 2) {
		__m128 d = _mm_loadu_ps((const float*)&data[i]);
		__m128 tw = _mm_castsi128_ps(_mm_set1_epi64x(*(const int64_t*)twiddle));
		__m128 tw_shuf = _mm_shuffle_ps(tw, tw, shuffle);

		__m128 mul_r = _mm_mul_ps(d, tw);
		__m128 mul_i = _mm_mul_ps(d, tw_shuf);

		__m128 mul_r_shuf = _mm_shuffle_ps(mul_r, mul_r, shuffle);
		__m128 mul_i_shuf = _mm_shuffle_ps(mul_i, mul_i, shuffle);

		__m128 result_r = _mm_sub_ps(mul_r, mul_r_shuf);
		__m128 result_i = _mm_add_ps(mul_i, mul_i_shuf);

		result_r = _mm_and_ps(mask, result_r);
		result_i = _mm_andnot_ps(mask, result_i);

		_mm_storeu_ps((float*)&data[i], _mm_or_ps(result_r, result_i));
	}

	for (; i < n; ++i) {
		RfReal32 re = data[i].re * twiddle->re - data[i].im * twiddle->im;
		RfReal32 im = data[i].re * twiddle->im + data[i].im * twiddle->re;
		data[i].re = re;
		data[i].im = im;
	}
}

void vec_op_sse::cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle)
{
	for (size_t i = 0; i < n; ++i) {
		__m128d a = _mm_loadu_pd((const double*)&data[i]);
		__m128d tw = _mm_loadu_pd((const double*)twiddle);

		__m128d a_swapped = _mm_shuffle_pd(a, a, 1);
		__m128d tw_swapped = _mm_shuffle_pd(tw, tw, 1);

		__m128d re_part = _mm_mul_pd(a, tw);
		__m128d im_part = _mm_mul_pd(a_swapped, tw_swapped);

		__m128d result = _mm_addsub_pd(re_part, im_part);

		_mm_storeu_pd((double*)&data[i], result);
	}
}

void vec_op_sse::cfft_bit_reverse(RfCplx32* data, size_t n)
{
	Helpers::cfft_bit_reverse(data, n);
}

void vec_op_sse::cfft_bit_reverse(RfCplx64* data, size_t n)
{
	Helpers::cfft_bit_reverse(data, n);
}

void vec_op_sse::cfft_scale(RfCplx32* data, size_t n, RfReal32 scale)
{
	__m128 scale_vec = _mm_set1_ps(scale);

	size_t i = 0;
	for (; i + 1 < n; i += 2) {
		__m128 d = _mm_loadu_ps((const float*)&data[i]);
		d = _mm_mul_ps(d, scale_vec);
		_mm_storeu_ps((float*)&data[i], d);
	}

	for (; i < n; ++i) {
		data[i].re *= scale;
		data[i].im *= scale;
	}
}

void vec_op_sse::cfft_scale(RfCplx64* data, size_t n, RfReal64 scale)
{
	__m128d scale_vec = _mm_set1_pd(scale);

	for (size_t i = 0; i < n; ++i) {
		__m128d d = _mm_loadu_pd((const double*)&data[i]);
		d = _mm_mul_pd(d, scale_vec);
		_mm_storeu_pd((double*)&data[i], d);
	}
}

// ============================================================
// Complete SIMD FFT implementations - SSE
// ============================================================

void vec_op_sse::cfvec_conj(RfCplx32* data, size_t n)
{
	// Mask to negate imaginary parts: [1, -1, 1, -1]
	const __m128 conj_mask = _mm_setr_ps(1.0f, -1.0f, 1.0f, -1.0f);
	
	size_t i = 0;
	for (; i + 2 <= n; i += 2) {
		__m128 d = _mm_loadu_ps((const float*)&data[i]);
		d = _mm_mul_ps(d, conj_mask);
		_mm_storeu_ps((float*)&data[i], d);
	}
	
	// Handle remaining element
	for (; i < n; ++i) {
		data[i].im = -data[i].im;
	}
}

void vec_op_sse::cfvec_conj(RfCplx64* data, size_t n)
{
	// Mask to negate imaginary part: [1, -1]
	const __m128d conj_mask = _mm_setr_pd(1.0, -1.0);
	
	for (size_t i = 0; i < n; ++i) {
		__m128d d = _mm_loadu_pd((const double*)&data[i]);
		d = _mm_mul_pd(d, conj_mask);
		_mm_storeu_pd((double*)&data[i], d);
	}
}

// SSE-optimized Cooley-Tukey FFT (float32)
void vec_op_sse::cfft_forward(RfCplx32* data, size_t n)
{
	if (n <= 1) return;
	
	// Bit-reversal permutation
	cfft_bit_reverse(data, n);
	
	// Compute log2(n)
	size_t log2n = 0;
	for (size_t temp = n; temp > 1; temp >>= 1) ++log2n;
	
	// Cooley-Tukey iterative FFT
	for (size_t s = 1; s <= log2n; ++s) {
		size_t m = size_t(1) << s;          // 2^s
		size_t half_m = m >> 1;             // m/2
		
		// Twiddle factor angle increment: -2*PI/m
		RfReal32 angle_step = static_cast<RfReal32>(-TWOPI / m);
		
		for (size_t k = 0; k < n; k += m) {
			// Process butterflies with SIMD where possible
			size_t j = 0;
			
			// Process pairs of butterflies with SSE (4 floats = 2 complex)
			for (; j + 1 < half_m; j += 2) {
				// Compute twiddle factors for j and j+1
				RfReal32 angle0 = angle_step * j;
				RfReal32 angle1 = angle_step * (j + 1);
				
				RfReal32 tw_re0 = std::cos(angle0);
				RfReal32 tw_im0 = std::sin(angle0);
				RfReal32 tw_re1 = std::cos(angle1);
				RfReal32 tw_im1 = std::sin(angle1);
				
				// Load even elements: data[k+j], data[k+j+1]
				__m128 even = _mm_loadu_ps((const float*)&data[k + j]);
				// Load odd elements: data[k+j+half_m], data[k+j+half_m+1]
				__m128 odd = _mm_loadu_ps((const float*)&data[k + j + half_m]);
				
				// Twiddle factors: [tw_re0, tw_im0, tw_re1, tw_im1]
				__m128 tw = _mm_setr_ps(tw_re0, tw_im0, tw_re1, tw_im1);
				
				// Complex multiplication: odd * twiddle
				// real part: odd.re * tw.re - odd.im * tw.im
				// imag part: odd.re * tw.im + odd.im * tw.re
				__m128 odd_re = _mm_shuffle_ps(odd, odd, _MM_SHUFFLE(2, 2, 0, 0)); // [re0, re0, re1, re1]
				__m128 odd_im = _mm_shuffle_ps(odd, odd, _MM_SHUFFLE(3, 3, 1, 1)); // [im0, im0, im1, im1]
				__m128 tw_swap = _mm_shuffle_ps(tw, tw, _MM_SHUFFLE(2, 3, 0, 1));  // [tw_im0, tw_re0, tw_im1, tw_re1]
				
				__m128 prod1 = _mm_mul_ps(odd_re, tw);      // [re0*tw_re0, re0*tw_im0, re1*tw_re1, re1*tw_im1]
				__m128 prod2 = _mm_mul_ps(odd_im, tw_swap); // [im0*tw_im0, im0*tw_re0, im1*tw_im1, im1*tw_re1]
				
				// Use addsub: result = prod1 +/- prod2 = [prod1[0]-prod2[0], prod1[1]+prod2[1], ...]
				__m128 t = _mm_addsub_ps(prod1, prod2);
				
				// Butterfly
				__m128 sum = _mm_add_ps(even, t);
				__m128 diff = _mm_sub_ps(even, t);
				
				_mm_storeu_ps((float*)&data[k + j], sum);
				_mm_storeu_ps((float*)&data[k + j + half_m], diff);
			}
			
			// Handle remaining butterfly (scalar)
			for (; j < half_m; ++j) {
				RfReal32 angle = angle_step * j;
				RfReal32 tw_re = std::cos(angle);
				RfReal32 tw_im = std::sin(angle);
				
				size_t even_idx = k + j;
				size_t odd_idx = k + j + half_m;
				
				RfReal32 t_re = data[odd_idx].re * tw_re - data[odd_idx].im * tw_im;
				RfReal32 t_im = data[odd_idx].re * tw_im + data[odd_idx].im * tw_re;
				
				data[odd_idx].re = data[even_idx].re - t_re;
				data[odd_idx].im = data[even_idx].im - t_im;
				data[even_idx].re += t_re;
				data[even_idx].im += t_im;
			}
		}
	}
}

void vec_op_sse::cfft_forward(RfCplx64* data, size_t n)
{
	if (n <= 1) return;
	
	// Bit-reversal permutation
	cfft_bit_reverse(data, n);
	
	// Compute log2(n)
	size_t log2n = 0;
	for (size_t temp = n; temp > 1; temp >>= 1) ++log2n;
	
	// Cooley-Tukey iterative FFT
	for (size_t s = 1; s <= log2n; ++s) {
		size_t m = size_t(1) << s;
		size_t half_m = m >> 1;
		
		RfReal64 angle_step = -TWOPI / m;
		
		for (size_t k = 0; k < n; k += m) {
			for (size_t j = 0; j < half_m; ++j) {
				RfReal64 angle = angle_step * j;
				RfReal64 tw_re = std::cos(angle);
				RfReal64 tw_im = std::sin(angle);
				
				size_t even_idx = k + j;
				size_t odd_idx = k + j + half_m;
				
				// Load odd element
				__m128d odd = _mm_loadu_pd((const double*)&data[odd_idx]);
				
				// Twiddle factor
				__m128d tw = _mm_setr_pd(tw_re, tw_im);
				__m128d tw_swap = _mm_setr_pd(tw_im, tw_re);
				
				// Complex multiplication
				__m128d odd_re = _mm_shuffle_pd(odd, odd, 0);       // [re, re]
				__m128d odd_im = _mm_shuffle_pd(odd, odd, 3);       // [im, im]
				
				__m128d prod1 = _mm_mul_pd(odd_re, tw);             // [re*tw_re, re*tw_im]
				__m128d prod2 = _mm_mul_pd(odd_im, tw_swap);        // [im*tw_im, im*tw_re]
				
				// t = [re*tw_re - im*tw_im, re*tw_im + im*tw_re]
				__m128d t = _mm_addsub_pd(prod1, prod2);
				
				// Butterfly
				__m128d even_v = _mm_loadu_pd((const double*)&data[even_idx]);
				__m128d sum = _mm_add_pd(even_v, t);
				__m128d diff = _mm_sub_pd(even_v, t);
				
				_mm_storeu_pd((double*)&data[even_idx], sum);
				_mm_storeu_pd((double*)&data[odd_idx], diff);
			}
		}
	}
}

void vec_op_sse::cfft_inverse(RfCplx32* data, size_t n)
{
	if (n <= 1) return;
	
	// Conjugate input
	cfvec_conj(data, n);
	
	// Forward FFT
	cfft_forward(data, n);
	
	// Conjugate and scale output
	cfvec_conj(data, n);
	cfft_scale(data, n, 1.0f / static_cast<RfReal32>(n));
}

void vec_op_sse::cfft_inverse(RfCplx64* data, size_t n)
{
	if (n <= 1) return;
	
	// Conjugate input
	cfvec_conj(data, n);
	
	// Forward FFT
	cfft_forward(data, n);
	
	// Conjugate and scale output
	cfvec_conj(data, n);
	cfft_scale(data, n, 1.0 / static_cast<RfReal64>(n));
}

void vec_op_sse::fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(in_vec1 + i); // Load 4 floats from in_vec1
		__m128 y_4 = _mm_loadu_ps(in_vec2 + i); // Load 4 floats from in_vec2
		__m128 z_4 = _mm_add_ps(x_4, y_4);      // Add corresponding elements
		_mm_storeu_ps(out_vec + i, z_4);        // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + in_vec2[i]; // Handle the remaining elements using scalar addition
	}
}

void vec_op_sse::fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(in_vec1 + i); // Load 4 floats from in_vec1
		__m128 y_4 = _mm_loadu_ps(in_vec2 + i); // Load 4 floats from in_vec2
		__m128 z_4 = _mm_mul_ps(x_4, y_4);      // Multiply corresponding elements
		_mm_storeu_ps(out_vec + i, z_4);        // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * in_vec2[i]; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_sse::fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size)
{
	__m128 add_ps = _mm_set1_ps(add); // Broadcast add to all elements of a SIMD register

	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(in_vec1 + i); // Load 4 floats from in_vec1
		__m128 y_4 = _mm_add_ps(x_4, add_ps);   // Add add_ps to each element of x_4
		_mm_storeu_ps(out_vec + i, y_4);        // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + add; // Handle the remaining elements using scalar addition
	}
}
void vec_op_sse::fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size)
{
	__m128 mul_ps = _mm_set1_ps(mul); // Broadcast mul to all elements of a SIMD register

	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(in_vec1 + i); // Load 4 floats from in_vec1
		__m128 y_4 = _mm_mul_ps(x_4, mul_ps);   // Multiply each element of x_4 with mul_ps
		_mm_storeu_ps(out_vec + i, y_4);        // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * mul; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_sse::fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(in_vec1 + i); // Load 4 floats from in_vec1
		__m128 y_4 = _mm_loadu_ps(in_vec2 + i); // Load 4 floats from in_vec2
		__m128 z_4 = _mm_sub_ps(x_4, y_4);      // Add corresponding elements
		_mm_storeu_ps(out_vec + i, z_4);        // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] - in_vec2[i]; // Handle the remaining elements using scalar addition
	}
}

void vec_op_sse::fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size)
{
	__m128 add_ps = _mm_set1_ps(sub); // Broadcast add to all elements of a SIMD register

	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(in_vec1 + i); // Load 4 floats from in_vec1
		__m128 y_4 = _mm_sub_ps(x_4, add_ps);   // Add add_ps to each element of x_4
		_mm_storeu_ps(out_vec + i, y_4);        // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] - sub; // Handle the remaining elements using scalar addition
	}
}

void vec_op_sse::fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(in_vec1 + i); // Load 4 floats from in_vec1
		__m128 y_4 = _mm_loadu_ps(in_vec2 + i); // Load 4 floats from in_vec2
		__m128 z_4 = _mm_div_ps(x_4, y_4);      // Multiply corresponding elements
		_mm_storeu_ps(out_vec + i, z_4);        // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] / in_vec2[i]; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_sse::fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size)
{
	__m128 mul_ps = _mm_set1_ps(div); // Broadcast mul to all elements of a SIMD register

	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(in_vec1 + i); // Load 4 floats from in_vec1
		__m128 y_4 = _mm_div_ps(x_4, mul_ps);   // Multiply each element of x_4 with mul_ps
		_mm_storeu_ps(out_vec + i, y_4);        // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] / div; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_sse::fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	size_t sse_size = size - size % 4;

	__m128 sum = _mm_setzero_ps();  // Initialize the sum register to 0

	for (int i = 0; i < sse_size; i += 4) {
		// Load 4 floats into a SIMD register
		__m128 values = _mm_loadu_ps(&in_vec[i]);

		// Add to the running sum
		sum = _mm_add_ps(sum, values);
	}

	// Extract the values from the SIMD register and sum them
	float temp[4];
	_mm_storeu_ps(temp, sum);
	*out = temp[0] + temp[1] + temp[2] + temp[3];

	// Remaining vectors using scaler add
	for (int i = sse_size; i < size; ++i) {
		*out += in_vec[i];
	}
}

void vec_op_sse::fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	size_t sse_size = size - size % 4;

	__m128 sum = _mm_setzero_ps();  // Initialize the sum register to 0

	for (int i = 0; i < sse_size; i += 4) {
		// Load 4 floats into a SIMD register
		__m128 values = _mm_loadu_ps(&in_vec[i]);

		// Compute the absolute values
		values = _mm_andnot_ps(_mm_set1_ps(-0.0f), values);

		// Add to the running sum
		sum = _mm_add_ps(sum, values);
	}

	// Extract the values from the SIMD register and sum them
	float temp[4];
	_mm_storeu_ps(temp, sum);
	*out = temp[0] + temp[1] + temp[2] + temp[3];

	// Remaining vectors using scaler add
	for (int i = sse_size; i < size; ++i) {
		*out += in_vec[i];
	}
}

void vec_op_sse::fvec_copy(const RfReal32* src, RfReal32* dst, size_t size)
{
	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		__m128 x_4 = _mm_loadu_ps(src + i);
		_mm_storeu_ps(dst + i, x_4);
	}

	for (i = simd_size; i < size; ++i) {
		dst[i] = src[i];
	}
}

void vec_op_sse::fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size)
{
	__m128 val_ps = _mm_set1_ps(*value);

	size_t i;
	size_t size_mod = size % 4;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 4) {
		_mm_storeu_ps(dst + i, val_ps);
	}

	for (i = simd_size; i < size; ++i) {
		dst[i] = *value;
	}
}

void vec_op_sse::dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 2;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 2) {
		__m128d x_2 = _mm_loadu_pd(in_vec1 + i); // Load 2 doubles from in_vec1
		__m128d y_2 = _mm_loadu_pd(in_vec2 + i); // Load 2 doubles from in_vec2
		__m128d z_2 = _mm_add_pd(x_2, y_2);      // Add corresponding elements
		_mm_storeu_pd(out_vec + i, z_2);         // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + in_vec2[i]; // Handle the remaining elements using scalar addition
	}
}

void vec_op_sse::dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 2;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 2) {
		__m128d x_2 = _mm_loadu_pd(in_vec1 + i); // Load 2 doubles from in_vec1
		__m128d y_2 = _mm_loadu_pd(in_vec2 + i); // Load 2 doubles from in_vec2
		__m128d z_2 = _mm_mul_pd(x_2, y_2);      // Multiply corresponding elements
		_mm_storeu_pd(out_vec + i, z_2);         // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * in_vec2[i]; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_sse::dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size)
{
	__m128d add_pd = _mm_set1_pd(add); // Broadcast add to all elements of an SSE register

	size_t i;
	size_t size_mod = size % 2;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 2) {
		__m128d x_2 = _mm_loadu_pd(in_vec1 + i); // Load 2 doubles from in_vec1
		__m128d y_2 = _mm_add_pd(x_2, add_pd);   // Add add_pd to each element of x_2
		_mm_storeu_pd(out_vec + i, y_2);         // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + add; // Handle the remaining elements using scalar addition
	}
}

void vec_op_sse::dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size)
{
	__m128d mul_pd = _mm_set1_pd(mul); // Broadcast mul to all elements of an SSE register

	size_t i;
	size_t size_mod = size % 2;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 2) {
		__m128d x_2 = _mm_loadu_pd(in_vec1 + i); // Load 2 doubles from in_vec1
		__m128d y_2 = _mm_mul_pd(x_2, mul_pd);   // Multiply each element of x_2 with mul_pd
		_mm_storeu_pd(out_vec + i, y_2);         // Store the result in out_vec
	}

	for (i = simd_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * mul; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_sse::dvec_copy(const RfReal64* src, RfReal64* dst, size_t size)
{
	size_t i;
	size_t size_mod = size % 2;
	size_t simd_size = size - size_mod;

	for (i = 0; i < simd_size; i += 2) {
		__m128d x_2 = _mm_loadu_pd(src + i); // Load 2 doubles from src
		_mm_storeu_pd(dst + i, x_2);          // Store directly to dst
	}

	for (i = simd_size; i < size; ++i) {
		dst[i] = src[i]; // Handle the remaining elements using scalar copy
	}
}

void vec_op_sse::cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size)
{
	int i = 0;
	const static int shuffle = _MM_SHUFFLE(2, 3, 0, 1);
	const static __m128 mask = _mm_castsi128_ps(_mm_set_epi32(0, RfUInt32_Max, 0, RfUInt32_Max));

	__m128 _vec1, _vec2, _vec2_shuf, mul_r, mul_i, result_r, result_i, output_vec;
	for (; i <= size - 2; i += 2) { // Process four complex numbers at a time

		_vec1 = _mm_loadu_ps((const float*)&in_vec1[i]);	// [x0, x1, x2, x3]
		_vec2 = _mm_loadu_ps((const float*)&in_vec2[i]);	// [y0, y1, y2, y3]

		_vec2_shuf = _mm_shuffle_ps(_vec2, _vec2, shuffle);	// Swap adjacent floats, yielding
		// [y1, y0, y3, y2]

		mul_r = _mm_mul_ps(_vec1, _vec2);		// Get real part (ac and bd)
		mul_i = _mm_mul_ps(_vec1, _vec2_shuf);	// Get imaginary part (ad and bc)

		__m128 mul_r_shuf = _mm_shuffle_ps(mul_r, mul_r, shuffle);
		__m128 mul_i_shuf = _mm_shuffle_ps(mul_i, mul_i, shuffle);

		result_r = _mm_sub_ps(mul_r, mul_r_shuf);
		result_i = _mm_add_ps(mul_i, mul_i_shuf);

		result_r = _mm_and_ps(mask, result_r);
		result_i = _mm_andnot_ps(mask, result_i);

		output_vec = _mm_or_ps(result_r, result_i);

		_mm_storeu_ps((float*)&out_vec[i], output_vec);
	}

	// Handle any remaining elements
	for (; i < size; ++i) {
		out_vec[i].re = in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im = in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

void vec_op_sse::cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size)
{
	int i = 0;
	for (; i <= size - 2; i += 2) { // Process two complex numbers at a time
		__m128d coeff_vec1 = _mm_loadu_pd((const double*)&in_vec1[i]);
		__m128d coeff_vec2 = _mm_loadu_pd((const double*)&in_vec1[i + 1]);
		__m128d delay_vec1 = _mm_loadu_pd((const double*)&in_vec2[i]);
		__m128d delay_vec2 = _mm_loadu_pd((const double*)&in_vec2[i + 1]);

		// Shuffle the delay vectors to align for complex multiplication
		__m128d delay_vec1_shuf = _mm_shuffle_pd(delay_vec1, delay_vec1, _MM_SHUFFLE2(0, 1));
		__m128d delay_vec2_shuf = _mm_shuffle_pd(delay_vec2, delay_vec2, _MM_SHUFFLE2(0, 1));

		// Perform the complex multiplication: (a + bi)(c + di) = (ac - bd) + (ad + bc)i
		__m128d real_part1 = _mm_sub_pd(_mm_mul_pd(coeff_vec1, delay_vec1), _mm_mul_pd(_mm_shuffle_pd(coeff_vec1, coeff_vec1, _MM_SHUFFLE2(0, 1)), delay_vec1_shuf));
		__m128d imag_part1 = _mm_add_pd(_mm_mul_pd(coeff_vec1, delay_vec1_shuf), _mm_mul_pd(_mm_shuffle_pd(coeff_vec1, coeff_vec1, _MM_SHUFFLE2(0, 1)), delay_vec1));

		__m128d real_part2 = _mm_sub_pd(_mm_mul_pd(coeff_vec2, delay_vec2), _mm_mul_pd(_mm_shuffle_pd(coeff_vec2, coeff_vec2, _MM_SHUFFLE2(0, 1)), delay_vec2_shuf));
		__m128d imag_part2 = _mm_add_pd(_mm_mul_pd(coeff_vec2, delay_vec2_shuf), _mm_mul_pd(_mm_shuffle_pd(coeff_vec2, coeff_vec2, _MM_SHUFFLE2(0, 1)), delay_vec2));

		// Save the results to the output vector
		_mm_storeu_pd((double*)&out_vec[i], _mm_unpacklo_pd(real_part1, imag_part1));
		_mm_storeu_pd((double*)&out_vec[i + 1], _mm_unpacklo_pd(real_part2, imag_part2));
	}

	// Handle any remaining elements
	for (; i < size; ++i) {
		out_vec[i].re = in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im = in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

void vec_op_sse::cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) {
	int i = 0;
	const static int shuffle = _MM_SHUFFLE(2, 3, 0, 1);
	const static __m128 mask = _mm_castsi128_ps(_mm_set_epi32(0, RfUInt32_Max, 0, RfUInt32_Max));

	__m128 _vec1, _vec2, _vec2_shuf, mul_r, mul_i, result_r, result_i, output_vec;
	for (; i <= size - 2; i += 2) { // Process four complex numbers at a time

		_vec1 = _mm_loadu_ps((const float*)&in_vec1[i]);	// [x0, x1, x2, x3]
		_vec2 = _mm_loadu_ps((const float*)&in_vec2[i]);	// [y0, y1, y2, y3]

		_vec2_shuf = _mm_shuffle_ps(_vec2, _vec2, shuffle);	// Swap adjacent floats, yielding
		// [y1, y0, y3, y2]

		mul_r = _mm_mul_ps(_vec1, _vec2);		// Get real part (ac and bd)
		mul_i = _mm_mul_ps(_vec1, _vec2_shuf);	// Get imaginary part (ad and bc)

		__m128 mul_r_shuf = _mm_shuffle_ps(mul_r, mul_r, shuffle);
		__m128 mul_i_shuf = _mm_shuffle_ps(mul_i, mul_i, shuffle);

		result_r = _mm_sub_ps(mul_r, mul_r_shuf);
		result_i = _mm_add_ps(mul_i, mul_i_shuf);

		result_r = _mm_and_ps(mask, result_r);
		result_i = _mm_andnot_ps(mask, result_i);

		output_vec = _mm_loadu_ps((const float*)&out_vec[i]);
		output_vec = _mm_add_ps(output_vec, _mm_or_ps(result_r, result_i));

		_mm_storeu_ps((float*)&out_vec[i], output_vec);
	}

	// Handle any remaining elements
	for (; i < size; ++i) {
		out_vec[i].re += in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im += in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}


void vec_op_sse::cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) {
	int i = 0;
	for (; i <= size - 2; i += 2) { // Process two complex numbers at a time
		__m128d coeff_vec1 = _mm_loadu_pd((const double*)&in_vec1[i]);
		__m128d coeff_vec2 = _mm_loadu_pd((const double*)&in_vec1[i + 1]);
		__m128d delay_vec1 = _mm_loadu_pd((const double*)&in_vec2[i]);
		__m128d delay_vec2 = _mm_loadu_pd((const double*)&in_vec2[i + 1]);

		// Shuffle the delay vectors to align for complex multiplication
		__m128d delay_vec1_shuf = _mm_shuffle_pd(delay_vec1, delay_vec1, _MM_SHUFFLE2(0, 1));
		__m128d delay_vec2_shuf = _mm_shuffle_pd(delay_vec2, delay_vec2, _MM_SHUFFLE2(0, 1));

		// Perform the complex multiplication: (a + bi)(c + di) = (ac - bd) + (ad + bc)i
		__m128d real_part1 = _mm_sub_pd(_mm_mul_pd(coeff_vec1, delay_vec1), _mm_mul_pd(_mm_shuffle_pd(coeff_vec1, coeff_vec1, _MM_SHUFFLE2(0, 1)), delay_vec1_shuf));
		__m128d imag_part1 = _mm_add_pd(_mm_mul_pd(coeff_vec1, delay_vec1_shuf), _mm_mul_pd(_mm_shuffle_pd(coeff_vec1, coeff_vec1, _MM_SHUFFLE2(0, 1)), delay_vec1));

		__m128d real_part2 = _mm_sub_pd(_mm_mul_pd(coeff_vec2, delay_vec2), _mm_mul_pd(_mm_shuffle_pd(coeff_vec2, coeff_vec2, _MM_SHUFFLE2(0, 1)), delay_vec2_shuf));
		__m128d imag_part2 = _mm_add_pd(_mm_mul_pd(coeff_vec2, delay_vec2_shuf), _mm_mul_pd(_mm_shuffle_pd(coeff_vec2, coeff_vec2, _MM_SHUFFLE2(0, 1)), delay_vec2));

		// Add the results to the output vector
		__m128d out_vec_part1 = _mm_loadu_pd((double*)&out_vec[i]);
		__m128d out_vec_part2 = _mm_loadu_pd((double*)&out_vec[i + 1]);

		out_vec_part1 = _mm_add_pd(out_vec_part1, _mm_unpacklo_pd(real_part1, imag_part1));
		out_vec_part2 = _mm_add_pd(out_vec_part2, _mm_unpacklo_pd(real_part2, imag_part2));

		_mm_storeu_pd((double*)&out_vec[i], out_vec_part1);
		_mm_storeu_pd((double*)&out_vec[i + 1], out_vec_part2);
	}

	// Handle any remaining elements
	for (; i < size; ++i) {
		out_vec[i].re += in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im += in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

// SIMD-optimized dot product for convolution
RfReal32 vec_op_sse::fconv_dot_product(const RfReal32* a, const RfReal32* b, size_t size)
{
	__m128 acc = _mm_setzero_ps();
	
	// Process 4 elements at a time
	size_t i = 0;
	for (; i + 4 <= size; i += 4) {
		__m128 va = _mm_loadu_ps(a + i);
		__m128 vb = _mm_loadu_ps(b + i);
		acc = _mm_add_ps(acc, _mm_mul_ps(va, vb));
	}
	
	// Horizontal sum
	acc = _mm_hadd_ps(acc, acc);
	acc = _mm_hadd_ps(acc, acc);
	RfReal32 result = _mm_cvtss_f32(acc);
	
	// Handle remaining elements
	for (; i < size; ++i) {
		result += a[i] * b[i];
	}
	
	return result;
}

RfReal64 vec_op_sse::dconv_dot_product(const RfReal64* a, const RfReal64* b, size_t size)
{
	__m128d acc = _mm_setzero_pd();
	
	// Process 2 elements at a time
	size_t i = 0;
	for (; i + 2 <= size; i += 2) {
		__m128d va = _mm_loadu_pd(a + i);
		__m128d vb = _mm_loadu_pd(b + i);
		acc = _mm_add_pd(acc, _mm_mul_pd(va, vb));
	}
	
	// Horizontal sum
	__m128d high = _mm_unpackhi_pd(acc, acc);
	acc = _mm_add_sd(acc, high);
	RfReal64 result = _mm_cvtsd_f64(acc);
	
	// Handle remaining elements
	for (; i < size; ++i) {
		result += a[i] * b[i];
	}
	
	return result;
}

void vec_op_sse::fconv_direct_kernel(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count)
{
	// Same as partitioned for direct method
	fconv_partition(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count, kernel_len);
}

void vec_op_sse::dconv_direct_kernel(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count)
{        
	// Same as partitioned for direct method
	dconv_partition(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count, kernel_len);
}

void vec_op_sse::fconv_partition(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count, RfInt32 partition_size)
{
	// SIMD-optimized partitioned convolution
	for (RfInt32 i = 0; i < count; ++i) {
		__m128 acc = _mm_setzero_ps();
		
		// Process kernel in chunks aligned with SIMD width (4 floats)
		RfInt32 k = 0;
		for (; k + 4 <= kernel_len; k += 4) {
			// Load 4 kernel samples
			__m128 k_vec = _mm_loadu_ps(kernel + k);
			
			// Load 4 history samples (with circular buffer handling)
			RfInt32 read_pos0 = (write_pos - 1 - k + hist_size) & hist_mask;
			RfInt32 read_pos1 = (write_pos - 1 - (k+1) + hist_size) & hist_mask;
			RfInt32 read_pos2 = (write_pos - 1 - (k+2) + hist_size) & hist_mask;
			RfInt32 read_pos3 = (write_pos - 1 - (k+3) + hist_size) & hist_mask;
			
			__m128 h_vec = _mm_set_ps(history[read_pos3], history[read_pos2], 
										history[read_pos1], history[read_pos0]);
			
			acc = _mm_add_ps(acc, _mm_mul_ps(k_vec, h_vec));
		}
		
		// Horizontal sum
		acc = _mm_hadd_ps(acc, acc);
		acc = _mm_hadd_ps(acc, acc);
		RfReal32 result = _mm_cvtss_f32(acc);
		
		// Handle remaining kernel samples
		for (; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			result += kernel[k] * history[read_pos];
		}
		
		output[i] = result;
		write_pos++;
	}
}

void vec_op_sse::dconv_partition(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count, RfInt32 partition_size)
{
	// SIMD-optimized partitioned convolution for doubles
	for (RfInt32 i = 0; i < count; ++i) {
		__m128d acc = _mm_setzero_pd();
		
		// Process kernel in chunks aligned with SIMD width (2 doubles)
		RfInt32 k = 0;
		for (; k + 2 <= kernel_len; k += 2) {
			// Load 2 kernel samples
			__m128d k_vec = _mm_loadu_pd(kernel + k);
			
			// Load 2 history samples (with circular buffer handling)
			RfInt32 read_pos0 = (write_pos - 1 - k + hist_size) & hist_mask;
			RfInt32 read_pos1 = (write_pos - 1 - (k+1) + hist_size) & hist_mask;
			
			__m128d h_vec = _mm_set_pd(history[read_pos1], history[read_pos0]);
			
			acc = _mm_add_pd(acc, _mm_mul_pd(k_vec, h_vec));
		}
		
		// Horizontal sum
		__m128d high = _mm_unpackhi_pd(acc, acc);
		acc = _mm_add_sd(acc, high);
		RfReal64 result = _mm_cvtsd_f64(acc);
		
		// Handle remaining kernel samples
		for (; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			result += kernel[k] * history[read_pos];
		}
		
		output[i] = result;
		write_pos++;
	}
}
#endif

// ------------------------------------------------------
// AVX vector operator
#ifdef RF_DETECT_AVX2

// ------------------------------------------------------
// AVX FFT operations
void vec_op_avx::cfft_butterfly(RfCplx32* data, size_t n, size_t stride)
{
	// Process 4 complex pairs (8 complex numbers) at a time
	for (size_t i = 0; i + 7 * stride < n; i += 8 * stride) {
		size_t idx0 = i;
		size_t idx4 = i + 4 * stride;

		__m256 a = _mm256_loadu_ps((const float*)&data[idx0]);
		__m256 b = _mm256_loadu_ps((const float*)&data[idx4]);

		// Butterfly: (a+b, a-b)
		__m256 sum = _mm256_add_ps(a, b);
		__m256 diff = _mm256_sub_ps(a, b);

		_mm256_storeu_ps((float*)&data[idx0], sum);
		_mm256_storeu_ps((float*)&data[idx4], diff);
	}

	// Handle remaining elements
	for (size_t i = (n / (8 * stride)) * (8 * stride); i + stride < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		RfReal32 re_a = data[idx1].re;
		RfReal32 im_a = data[idx1].im;
		RfReal32 re_b = data[idx2].re;
		RfReal32 im_b = data[idx2].im;

		data[idx1].re = re_a + re_b;
		data[idx1].im = im_a + im_b;
		data[idx2].re = re_a - re_b;
		data[idx2].im = im_a - im_b;
	}
}

void vec_op_avx::cfft_butterfly(RfCplx64* data, size_t n, size_t stride)
{
	for (size_t i = 0; i + 3 * stride < n; i += 4 * stride) {
		size_t idx0 = i;
		size_t idx2 = i + 2 * stride;

		__m256d a = _mm256_loadu_pd((const double*)&data[idx0]);
		__m256d b = _mm256_loadu_pd((const double*)&data[idx2]);

		__m256d sum = _mm256_add_pd(a, b);
		__m256d diff = _mm256_sub_pd(a, b);

		_mm256_storeu_pd((double*)&data[idx0], sum);
		_mm256_storeu_pd((double*)&data[idx2], diff);
	}

	// Handle remaining elements
	for (size_t i = (n / (4 * stride)) * (4 * stride); i + stride < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		__m128d a = _mm_loadu_pd((const double*)&data[idx1]);
		__m128d b = _mm_loadu_pd((const double*)&data[idx2]);

		__m128d sum = _mm_add_pd(a, b);
		__m128d diff = _mm_sub_pd(a, b);

		_mm_storeu_pd((double*)&data[idx1], sum);
		_mm_storeu_pd((double*)&data[idx2], diff);
	}
}

void vec_op_avx::cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle)
{
	const static int shuffle = _MM_SHUFFLE(2, 3, 0, 1);
	const static __m256 mask = _mm256_castsi256_ps(_mm256_set_epi32(0, RfUInt32_Max, 0, RfUInt32_Max, 0, RfUInt32_Max, 0, RfUInt32_Max));

	__m256 tw = _mm256_castsi256_ps(_mm256_set1_epi64x(*(const int64_t*)twiddle));

	size_t i = 0;
	for (; i + 4 < n; i += 4) {
		__m256 d = _mm256_loadu_ps((const float*)&data[i]);
		__m256 tw_shuf = _mm256_permute_ps(tw, shuffle);

		__m256 mul_r = _mm256_mul_ps(d, tw);
		__m256 mul_i = _mm256_mul_ps(d, tw_shuf);

		__m256 mul_r_shuf = _mm256_permute_ps(mul_r, shuffle);
		__m256 mul_i_shuf = _mm256_permute_ps(mul_i, shuffle);

		__m256 result_r = _mm256_sub_ps(mul_r, mul_r_shuf);
		__m256 result_i = _mm256_add_ps(mul_i, mul_i_shuf);

		result_r = _mm256_and_ps(mask, result_r);
		result_i = _mm256_andnot_ps(mask, result_i);

		_mm256_storeu_ps((float*)&data[i], _mm256_or_ps(result_r, result_i));
	}

	for (; i < n; ++i) {
		RfReal32 re = data[i].re * twiddle->re - data[i].im * twiddle->im;
		RfReal32 im = data[i].re * twiddle->im + data[i].im * twiddle->re;
		data[i].re = re;
		data[i].im = im;
	}
}

void vec_op_avx::cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle)
{
	for (size_t i = 0; i + 1 < n; i += 2) {
		__m256d a = _mm256_loadu_pd((const double*)&data[i]);
		__m256d tw = _mm256_broadcast_pd((__m128d*)twiddle);

		__m256d a_swapped = _mm256_permute_pd(a, 5);

		__m256d re_part = _mm256_mul_pd(a, tw);
		__m256d im_part = _mm256_mul_pd(a_swapped, tw);

		__m256d result = _mm256_addsub_pd(re_part, im_part);

		_mm256_storeu_pd((double*)&data[i], result);
	}

	// Handle remaining element
	if (n % 2 != 0) {
		size_t i = n - 1;
		RfReal64 re = data[i].re * twiddle->re - data[i].im * twiddle->im;
		RfReal64 im = data[i].re * twiddle->im + data[i].im * twiddle->re;
		data[i].re = re;
		data[i].im = im;
	}
}

void vec_op_avx::cfft_bit_reverse(RfCplx32* data, size_t n)
{
	Helpers::cfft_bit_reverse(data, n);
}

void vec_op_avx::cfft_bit_reverse(RfCplx64* data, size_t n)
{
	Helpers::cfft_bit_reverse(data, n);
}

void vec_op_avx::cfft_scale(RfCplx32* data, size_t n, RfReal32 scale)
{
	__m256 scale_vec = _mm256_set1_ps(scale);

	size_t i = 0;
	for (; i + 4 < n; i += 4) {
		__m256 d = _mm256_loadu_ps((const float*)&data[i]);
		d = _mm256_mul_ps(d, scale_vec);
		_mm256_storeu_ps((float*)&data[i], d);
	}

	for (; i < n; ++i) {
		data[i].re *= scale;
		data[i].im *= scale;
	}
}

void vec_op_avx::cfft_scale(RfCplx64* data, size_t n, RfReal64 scale)
{
	__m256d scale_vec = _mm256_set1_pd(scale);

	size_t i = 0;
	for (; i + 2 < n; i += 2) {
		__m256d d = _mm256_loadu_pd((const double*)&data[i]);
		d = _mm256_mul_pd(d, scale_vec);
		_mm256_storeu_pd((double*)&data[i], d);
	}

	for (; i < n; ++i) {
		__m128d d = _mm_loadu_pd((const double*)&data[i]);
		__m128d scale_sse = _mm_set1_pd(scale);
		d = _mm_mul_pd(d, scale_sse);
		_mm_storeu_pd((double*)&data[i], d);
	}
}

// ============================================================
// Complete SIMD FFT implementations - AVX
// ============================================================

void vec_op_avx::cfvec_conj(RfCplx32* data, size_t n)
{
	// Mask to negate imaginary parts: [1, -1, 1, -1, 1, -1, 1, -1]
	const __m256 conj_mask = _mm256_setr_ps(1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f, -1.0f);
	
	size_t i = 0;
	for (; i + 4 <= n; i += 4) {
		__m256 d = _mm256_loadu_ps((const float*)&data[i]);
		d = _mm256_mul_ps(d, conj_mask);
		_mm256_storeu_ps((float*)&data[i], d);
	}
	
	// Handle remaining elements with SSE
	for (; i + 2 <= n; i += 2) {
		__m128 d = _mm_loadu_ps((const float*)&data[i]);
		__m128 mask = _mm_setr_ps(1.0f, -1.0f, 1.0f, -1.0f);
		d = _mm_mul_ps(d, mask);
		_mm_storeu_ps((float*)&data[i], d);
	}
	
	// Handle final remaining element
	for (; i < n; ++i) {
		data[i].im = -data[i].im;
	}
}

void vec_op_avx::cfvec_conj(RfCplx64* data, size_t n)
{
	// Mask to negate imaginary parts: [1, -1, 1, -1]
	const __m256d conj_mask = _mm256_setr_pd(1.0, -1.0, 1.0, -1.0);
	
	size_t i = 0;
	for (; i + 2 <= n; i += 2) {
		__m256d d = _mm256_loadu_pd((const double*)&data[i]);
		d = _mm256_mul_pd(d, conj_mask);
		_mm256_storeu_pd((double*)&data[i], d);
	}
	
	// Handle remaining element
	for (; i < n; ++i) {
		data[i].im = -data[i].im;
	}
}

// AVX-optimized Cooley-Tukey FFT (float32)
void vec_op_avx::cfft_forward(RfCplx32* data, size_t n)
{
	if (n <= 1) return;
	
	// Bit-reversal permutation
	cfft_bit_reverse(data, n);
	
	// Compute log2(n)
	size_t log2n = 0;
	for (size_t temp = n; temp > 1; temp >>= 1) ++log2n;
	
	// Cooley-Tukey iterative FFT
	for (size_t s = 1; s <= log2n; ++s) {
		size_t m = size_t(1) << s;
		size_t half_m = m >> 1;
		
		RfReal32 angle_step = static_cast<RfReal32>(-TWOPI / m);
		
		for (size_t k = 0; k < n; k += m) {
			size_t j = 0;
			
			// Process 4 butterflies at once with AVX (8 floats = 4 complex)
			for (; j + 3 < half_m; j += 4) {
				// Compute twiddle factors for j, j+1, j+2, j+3
				RfReal32 angle0 = angle_step * j;
				RfReal32 angle1 = angle_step * (j + 1);
				RfReal32 angle2 = angle_step * (j + 2);
				RfReal32 angle3 = angle_step * (j + 3);
				
				RfReal32 tw_re0 = std::cos(angle0), tw_im0 = std::sin(angle0);
				RfReal32 tw_re1 = std::cos(angle1), tw_im1 = std::sin(angle1);
				RfReal32 tw_re2 = std::cos(angle2), tw_im2 = std::sin(angle2);
				RfReal32 tw_re3 = std::cos(angle3), tw_im3 = std::sin(angle3);
				
				// Load 4 even elements
				__m256 even = _mm256_loadu_ps((const float*)&data[k + j]);
				// Load 4 odd elements
				__m256 odd = _mm256_loadu_ps((const float*)&data[k + j + half_m]);
				
				// Twiddle factors: [tw_re0, tw_im0, tw_re1, tw_im1, tw_re2, tw_im2, tw_re3, tw_im3]
				__m256 tw = _mm256_setr_ps(tw_re0, tw_im0, tw_re1, tw_im1, tw_re2, tw_im2, tw_re3, tw_im3);
				
				// Complex multiplication: odd * twiddle
				// Shuffle to get [re, re, re, re, ...] and [im, im, im, im, ...]
				__m256 odd_re = _mm256_shuffle_ps(odd, odd, _MM_SHUFFLE(2, 2, 0, 0)); // [re0, re0, re1, re1, re2, re2, re3, re3]
				__m256 odd_im = _mm256_shuffle_ps(odd, odd, _MM_SHUFFLE(3, 3, 1, 1)); // [im0, im0, im1, im1, im2, im2, im3, im3]
				__m256 tw_swap = _mm256_shuffle_ps(tw, tw, _MM_SHUFFLE(2, 3, 0, 1));  // [tw_im, tw_re, ...]
				
				__m256 prod1 = _mm256_mul_ps(odd_re, tw);
				__m256 prod2 = _mm256_mul_ps(odd_im, tw_swap);
				
				// Use addsub for complex multiplication result
				__m256 t = _mm256_addsub_ps(prod1, prod2);
				
				// Butterfly
				__m256 sum = _mm256_add_ps(even, t);
				__m256 diff = _mm256_sub_ps(even, t);
				
				_mm256_storeu_ps((float*)&data[k + j], sum);
				_mm256_storeu_ps((float*)&data[k + j + half_m], diff);
			}
			
			// Handle remaining butterflies with SSE or scalar
			for (; j < half_m; ++j) {
				RfReal32 angle = angle_step * j;
				RfReal32 tw_re = std::cos(angle);
				RfReal32 tw_im = std::sin(angle);
				
				size_t even_idx = k + j;
				size_t odd_idx = k + j + half_m;
				
				RfReal32 t_re = data[odd_idx].re * tw_re - data[odd_idx].im * tw_im;
				RfReal32 t_im = data[odd_idx].re * tw_im + data[odd_idx].im * tw_re;
				
				data[odd_idx].re = data[even_idx].re - t_re;
				data[odd_idx].im = data[even_idx].im - t_im;
				data[even_idx].re += t_re;
				data[even_idx].im += t_im;
			}
		}
	}
}

void vec_op_avx::cfft_forward(RfCplx64* data, size_t n)
{
	if (n <= 1) return;
	
	// Bit-reversal permutation
	cfft_bit_reverse(data, n);
	
	// Compute log2(n)
	size_t log2n = 0;
	for (size_t temp = n; temp > 1; temp >>= 1) ++log2n;
	
	// Cooley-Tukey iterative FFT
	for (size_t s = 1; s <= log2n; ++s) {
		size_t m = size_t(1) << s;
		size_t half_m = m >> 1;
		
		RfReal64 angle_step = -TWOPI / m;
		
		for (size_t k = 0; k < n; k += m) {
			size_t j = 0;
			
			// Process 2 butterflies at once with AVX (4 doubles = 2 complex)
			for (; j + 1 < half_m; j += 2) {
				RfReal64 angle0 = angle_step * j;
				RfReal64 angle1 = angle_step * (j + 1);
				
				RfReal64 tw_re0 = std::cos(angle0), tw_im0 = std::sin(angle0);
				RfReal64 tw_re1 = std::cos(angle1), tw_im1 = std::sin(angle1);
				
				// Load 2 even elements
				__m256d even = _mm256_loadu_pd((const double*)&data[k + j]);
				// Load 2 odd elements
				__m256d odd = _mm256_loadu_pd((const double*)&data[k + j + half_m]);
				
				// Twiddle factors
				__m256d tw = _mm256_setr_pd(tw_re0, tw_im0, tw_re1, tw_im1);
				
				// Complex multiplication
				__m256d odd_re = _mm256_shuffle_pd(odd, odd, 0x0);    // [re0, re0, re1, re1]
				__m256d odd_im = _mm256_shuffle_pd(odd, odd, 0xF);    // [im0, im0, im1, im1]
				__m256d tw_swap = _mm256_shuffle_pd(tw, tw, 0x5);     // [tw_im0, tw_re0, tw_im1, tw_re1]
				
				__m256d prod1 = _mm256_mul_pd(odd_re, tw);
				__m256d prod2 = _mm256_mul_pd(odd_im, tw_swap);
				
				__m256d t = _mm256_addsub_pd(prod1, prod2);
				
				// Butterfly
				__m256d sum = _mm256_add_pd(even, t);
				__m256d diff = _mm256_sub_pd(even, t);
				
				_mm256_storeu_pd((double*)&data[k + j], sum);
				_mm256_storeu_pd((double*)&data[k + j + half_m], diff);
			}
			
			// Handle remaining butterfly (scalar)
			for (; j < half_m; ++j) {
				RfReal64 angle = angle_step * j;
				RfReal64 tw_re = std::cos(angle);
				RfReal64 tw_im = std::sin(angle);
				
				size_t even_idx = k + j;
				size_t odd_idx = k + j + half_m;
				
				RfReal64 t_re = data[odd_idx].re * tw_re - data[odd_idx].im * tw_im;
				RfReal64 t_im = data[odd_idx].re * tw_im + data[odd_idx].im * tw_re;
				
				data[odd_idx].re = data[even_idx].re - t_re;
				data[odd_idx].im = data[even_idx].im - t_im;
				data[even_idx].re += t_re;
				data[even_idx].im += t_im;
			}
		}
	}
}

void vec_op_avx::cfft_inverse(RfCplx32* data, size_t n)
{
	if (n <= 1) return;
	
	cfvec_conj(data, n);
	cfft_forward(data, n);
	cfvec_conj(data, n);
	cfft_scale(data, n, 1.0f / static_cast<RfReal32>(n));
}

void vec_op_avx::cfft_inverse(RfCplx64* data, size_t n)
{
	if (n <= 1) return;
	
	cfvec_conj(data, n);
	cfft_forward(data, n);
	cfvec_conj(data, n);
	cfft_scale(data, n, 1.0 / static_cast<RfReal64>(n));
}

void vec_op_avx::fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(in_vec1 + i); // Load 8 floats from in_vec1
		__m256 y_8 = _mm256_loadu_ps(in_vec2 + i); // Load 8 floats from in_vec2
		__m256 z_8 = _mm256_add_ps(x_8, y_8);      // Add corresponding elements
		_mm256_storeu_ps(out_vec + i, z_8);        // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + in_vec2[i]; // Handle the remaining elements using scalar addition
	}
}

void vec_op_avx::fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(in_vec1 + i); // Load 8 floats from in_vec1
		__m256 y_8 = _mm256_loadu_ps(in_vec2 + i); // Load 8 floats from in_vec2
		__m256 z_8 = _mm256_mul_ps(x_8, y_8);      // Multiply corresponding elements
		_mm256_storeu_ps(out_vec + i, z_8);        // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * in_vec2[i]; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_avx::fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size)
{
	__m256 add_ps = _mm256_set1_ps(add); // Broadcast add to all elements of an AVX register

	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(in_vec1 + i); // Load 8 floats from in_vec1
		__m256 y_8 = _mm256_add_ps(x_8, add_ps);   // Add add_ps to each element of x_8
		_mm256_storeu_ps(out_vec + i, y_8);        // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + add; // Handle the remaining elements using scalar addition
	}
}

void vec_op_avx::fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size)
{
	__m256 mul_ps = _mm256_set1_ps(mul); // Broadcast mul to all elements of an AVX register

	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(in_vec1 + i); // Load 8 floats from in_vec1
		__m256 y_8 = _mm256_mul_ps(x_8, mul_ps);   // Multiply each element of x_8 with mul_ps
		_mm256_storeu_ps(out_vec + i, y_8);        // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * mul; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_avx::fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(in_vec1 + i); // Load 8 floats from in_vec1
		__m256 y_8 = _mm256_loadu_ps(in_vec2 + i); // Load 8 floats from in_vec2
		__m256 z_8 = _mm256_sub_ps(x_8, y_8);      // Add corresponding elements
		_mm256_storeu_ps(out_vec + i, z_8);        // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] - in_vec2[i]; // Handle the remaining elements using scalar addition
	}
}

void vec_op_avx::fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size)
{
	__m256 add_ps = _mm256_set1_ps(sub); // Broadcast add to all elements of an AVX register

	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(in_vec1 + i); // Load 8 floats from in_vec1
		__m256 y_8 = _mm256_sub_ps(x_8, add_ps);   // Add add_ps to each element of x_8
		_mm256_storeu_ps(out_vec + i, y_8);        // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] - sub; // Handle the remaining elements using scalar addition
	}
}

void vec_op_avx::fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(in_vec1 + i); // Load 8 floats from in_vec1
		__m256 y_8 = _mm256_loadu_ps(in_vec2 + i); // Load 8 floats from in_vec2
		__m256 z_8 = _mm256_div_ps(x_8, y_8);      // Multiply corresponding elements
		_mm256_storeu_ps(out_vec + i, z_8);        // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] / in_vec2[i]; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_avx::fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size)
{
	__m256 mul_ps = _mm256_set1_ps(div); // Broadcast mul to all elements of an AVX register

	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(in_vec1 + i); // Load 8 floats from in_vec1
		__m256 y_8 = _mm256_div_ps(x_8, mul_ps);   // Multiply each element of x_8 with mul_ps
		_mm256_storeu_ps(out_vec + i, y_8);        // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] / div; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_avx::fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	size_t avx_size = size - size % 8;
	__m256 sum = _mm256_setzero_ps();  // Initialize the sum register to 0

	for (int i = 0; i < avx_size; i += 8) {
		// Load 8 floats into a SIMD register
		__m256 values = _mm256_loadu_ps(&in_vec[i]);

		// Add to the running sum
		sum = _mm256_add_ps(sum, values);
	}

	// Horizontal sum of the SIMD register
	__m128 sumLow = _mm256_castps256_ps128(sum);
	__m128 sumHigh = _mm256_extractf128_ps(sum, 1);
	sumLow = _mm_add_ps(sumLow, sumHigh);
	sumLow = _mm_hadd_ps(sumLow, sumLow);
	sumLow = _mm_hadd_ps(sumLow, sumLow);
	*out = _mm_cvtss_f32(sumLow);

	// Remaining vectors using scaler add
	for (int i = avx_size; i < size; ++i) {
		*out += std::abs(in_vec[i]);
	}
}

void vec_op_avx::fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	size_t avx_size = size - size % 8;
	__m256 sum = _mm256_setzero_ps();  // Initialize the sum register to 0

	for (int i = 0; i < avx_size; i += 8) {
		// Load 8 floats into a SIMD register
		__m256 values = _mm256_loadu_ps(&in_vec[i]);

		// Compute the absolute values
		values = _mm256_andnot_ps(_mm256_set1_ps(-0.0f), values);

		// Add to the running sum
		sum = _mm256_add_ps(sum, values);
	}

	// Horizontal sum of the SIMD register
	__m128 sumLow = _mm256_castps256_ps128(sum);
	__m128 sumHigh = _mm256_extractf128_ps(sum, 1);
	sumLow = _mm_add_ps(sumLow, sumHigh);
	sumLow = _mm_hadd_ps(sumLow, sumLow);
	sumLow = _mm_hadd_ps(sumLow, sumLow);
	*out = _mm_cvtss_f32(sumLow);

	// Remaining vectors using scaler add
	for (int i = avx_size; i < size; ++i) {
		*out += std::abs(in_vec[i]);
	}
}

void vec_op_avx::fvec_copy(const RfReal32* src, RfReal32* dst, size_t size)
{
	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		__m256 x_8 = _mm256_loadu_ps(src + i);
		_mm256_storeu_ps(dst + i, x_8);
	}

	for (i = avx_size; i < size; ++i) {
		dst[i] = src[i];
	}
}

void vec_op_avx::fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size)
{
	__m256 val_ps = _mm256_set1_ps(*value);

	size_t i;
	size_t size_mod = size % 8;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 8) {
		_mm256_storeu_ps(dst + i, val_ps);
	}

	for (i = avx_size; i < size; ++i) {
		dst[i] = *value;
	}
}

void vec_op_avx::dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 4;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 4) {
		__m256d x_4 = _mm256_loadu_pd(in_vec1 + i); // Load 4 doubles from in_vec1
		__m256d y_4 = _mm256_loadu_pd(in_vec2 + i); // Load 4 doubles from in_vec2
		__m256d z_4 = _mm256_add_pd(x_4, y_4);      // Add corresponding elements
		_mm256_storeu_pd(out_vec + i, z_4);         // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + in_vec2[i]; // Handle the remaining elements using scalar addition
	}
}

void vec_op_avx::dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	size_t i;
	size_t size_mod = size % 4;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 4) {
		__m256d x_4 = _mm256_loadu_pd(in_vec1 + i); // Load 4 doubles from in_vec1
		__m256d y_4 = _mm256_loadu_pd(in_vec2 + i); // Load 4 doubles from in_vec2
		__m256d z_4 = _mm256_mul_pd(x_4, y_4);      // Multiply corresponding elements
		_mm256_storeu_pd(out_vec + i, z_4);         // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * in_vec2[i]; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_avx::dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size)
{
	__m256d add_pd = _mm256_set1_pd(add); // Broadcast add to all elements of an AVX register

	size_t i;
	size_t size_mod = size % 4;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 4) {
		__m256d x_4 = _mm256_loadu_pd(in_vec1 + i); // Load 4 doubles from in_vec1
		__m256d y_4 = _mm256_add_pd(x_4, add_pd);   // Add add_pd to each element of x_4
		_mm256_storeu_pd(out_vec + i, y_4);         // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + add; // Handle the remaining elements using scalar addition
	}
}

void vec_op_avx::dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size)
{
	__m256d mul_pd = _mm256_set1_pd(mul); // Broadcast mul to all elements of an AVX register

	size_t i;
	size_t size_mod = size % 4;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 4) {
		__m256d x_4 = _mm256_loadu_pd(in_vec1 + i); // Load 4 doubles from in_vec1
		__m256d y_4 = _mm256_mul_pd(x_4, mul_pd);   // Multiply each element of x_4 with mul_pd
		_mm256_storeu_pd(out_vec + i, y_4);         // Store the result in out_vec
	}

	for (i = avx_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * mul; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_avx::dvec_copy(const RfReal64* src, RfReal64* dst, size_t size)
{
	size_t i;
	size_t size_mod = size % 4;
	size_t avx_size = size - size_mod;

	for (i = 0; i < avx_size; i += 4) {
		__m256d x_4 = _mm256_loadu_pd(src + i); // Load 4 doubles from src
		_mm256_storeu_pd(dst + i, x_4);          // Store directly to dst
	}

	for (i = avx_size; i < size; ++i) {
		dst[i] = src[i]; // Handle the remaining elements using scalar copy
	}
}

void vec_op_avx::cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size)
{
	int i = 0;
	const static int shuffle = _MM_SHUFFLE(2, 3, 0, 1);
	const static __m256 mask = _mm256_castsi256_ps(_mm256_set_epi32(0, RfUInt32_Max, 0, RfUInt32_Max, 0, RfUInt32_Max, 0, RfUInt32_Max));
	__m256 _vec1, _vec2, _vec2_shuf, mul_r, mul_i, result_r, result_i, output_vec;
	for (; i <= size - 4; i += 4) { // Process four complex numbers at a time

		_vec1 = _mm256_loadu_ps((const float*)&in_vec1[i]);	// [x0, x1, x2, x3, x4, x5, x6, x7]
		_vec2 = _mm256_loadu_ps((const float*)&in_vec2[i]);	// [y0, y1, y2, y3, y4, y5, y6, y7]

		_vec2_shuf = _mm256_permute_ps(_vec2, shuffle);	// Swap adjacent floats, yielding \
																// [y1, y0, y3, y2, y5, y4, y7, y6]

		mul_r = _mm256_mul_ps(_vec1, _vec2);		// Get real part (ac and bd)
		mul_i = _mm256_mul_ps(_vec1, _vec2_shuf);	// Get imaginary part (ad and bc)

		result_r = _mm256_sub_ps(mul_r, _mm256_permute_ps(mul_r, shuffle));
		result_i = _mm256_add_ps(mul_i, _mm256_permute_ps(mul_i, shuffle));

		result_r = _mm256_and_ps(mask, result_r);
		result_i = _mm256_andnot_ps(mask, result_i);

		output_vec = _mm256_or_ps(result_r, result_i);

		_mm256_storeu_ps((float*)&out_vec[i], output_vec);
	}

	// Handle any remaining elements
	for (; i < size; ++i) {
		out_vec[i].re = in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im = in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

void vec_op_avx::cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size)
{
	// Complex double vector multiplication using AVX
	// Based on interleaved complex multiplication algorithm
	// Processes 2 complex numbers at a time (4 doubles per 256-bit register)
	int i = 0;
	constexpr int SWAP_RE_IM = 0x5; // 0b0101: swaps within each 128-bit lane

	for (; i <= size - 2; i += 2) {
		// Load [ar0, ai0, ar1, ai1] from in_vec1
		__m256d va = _mm256_loadu_pd((const double*)&in_vec1[i]);
		// Load [cr0, ci0, cr1, ci1] from in_vec2
		__m256d vb = _mm256_loadu_pd((const double*)&in_vec2[i]);

		// Duplicate real parts: [ar0, ar0, ar1, ar1]
		__m256d va_r = _mm256_unpacklo_pd(va, va);

		// Duplicate imag parts: [ai0, ai0, ai1, ai1]
		__m256d va_i = _mm256_unpackhi_pd(va, va);

		// Swap real/imag of vb within each complex: [ci0, cr0, ci1, cr1]
		__m256d vb_sw = _mm256_permute_pd(vb, SWAP_RE_IM);

		// p1 = ar * (cr, ci)
		__m256d p1 = _mm256_mul_pd(va_r, vb);

		// p2 = ai * (ci, cr)
		__m256d p2 = _mm256_mul_pd(va_i, vb_sw);

		// (ar*cr - ai*ci), (ar*ci + ai*cr) for each complex
		// _mm256_addsub_pd adds the first pair and subtracts the second pair
		__m256d vr = _mm256_addsub_pd(p1, p2);

		_mm256_storeu_pd((double*)&out_vec[i], vr);
	}

	// Handle any remaining elements
	for (; i < size; ++i) {
		out_vec[i].re = in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im = in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

void vec_op_avx::cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) {
	// AVX2 + FMA3 complex multiply-accumulate: out += in1 * in2
	// Uses the addsub + dup pattern (same as cdvec_mlt_add double version) for float:
	//   real_part = ar*cr - ai*ci  →  fmsub(ar, cr, ai*ci)
	//   imag_part = ar*ci + ai*cr  →  fmadd(ar, ci, ai*cr)
	// _mm256_addsub_ps(p1, p2) = [ p1[0]-p2[0], p1[1]+p2[1], ... ] handles both in one instruction.
	constexpr int SWAP_RE_IM = _MM_SHUFFLE(2, 3, 0, 1); // swap adjacent pairs

	int i = 0;
	for (; i <= (int)size - 4; i += 4) {
		// Load 4 complex floats: [ar0,ai0, ar1,ai1, ar2,ai2, ar3,ai3]
		__m256 va = _mm256_loadu_ps((const float*)&in_vec1[i]);
		__m256 vb = _mm256_loadu_ps((const float*)&in_vec2[i]);

		// Duplicate real parts of va: [ar0,ar0, ar1,ar1, ar2,ar2, ar3,ar3]
		__m256 va_r = _mm256_moveldup_ps(va);
		// Duplicate imag parts of va: [ai0,ai0, ai1,ai1, ai2,ai2, ai3,ai3]
		__m256 va_i = _mm256_movehdup_ps(va);

		// Swap real/imag of vb: [bi0,br0, bi1,br1, bi2,br2, bi3,br3]
		__m256 vb_sw = _mm256_permute_ps(vb, SWAP_RE_IM);

		// p1 = ar * (cr, ci)  — real parts multiply both re and im of vb
		// p2 = ai * (ci, cr)  — imag parts multiply swapped vb
		// addsub: result = (ar*cr - ai*ci,  ar*ci + ai*cr) per complex  [FMA3]
		__m256 p1 = _mm256_mul_ps(va_r, vb);
		__m256 result = _mm256_fmaddsub_ps(va_i, vb_sw, p1);

		// Accumulate into output
		__m256 vo = _mm256_loadu_ps((const float*)&out_vec[i]);
		vo = _mm256_add_ps(vo, result);
		_mm256_storeu_ps((float*)&out_vec[i], vo);
	}

	// Handle remaining elements
	for (; i < (int)size; ++i) {
		out_vec[i].re += in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im += in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

void vec_op_avx::cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) {
	// Complex double vector multiply-add using AVX
	// out += in1 * in2
	int i = 0;
	constexpr int SWAP_RE_IM = 0x5;

	for (; i <= size - 2; i += 2) {
		// Load [ar0, ai0, ar1, ai1] from in_vec1
		__m256d va = _mm256_loadu_pd((const double*)&in_vec1[i]);
		// Load [cr0, ci0, cr1, ci1] from in_vec2
		__m256d vb = _mm256_loadu_pd((const double*)&in_vec2[i]);

		// Duplicate real parts
		__m256d va_r = _mm256_unpacklo_pd(va, va);
		// Duplicate imag parts
		__m256d va_i = _mm256_unpackhi_pd(va, va);

		// Swap real/imag of vb within each complex
		__m256d vb_sw = _mm256_permute_pd(vb, SWAP_RE_IM);

		// p1 = ar * (cr, ci)
		__m256d p1 = _mm256_mul_pd(va_r, vb);
		// p2 = ai * (ci, cr)
		__m256d p2 = _mm256_mul_pd(va_i, vb_sw);

		// (ar*cr - ai*ci), (ar*ci + ai*cr) for each complex
		__m256d vr = _mm256_addsub_pd(p1, p2);

		// Load existing output and add result
		__m256d output_vec = _mm256_loadu_pd((const double*)&out_vec[i]);
		output_vec = _mm256_add_pd(output_vec, vr);

		_mm256_storeu_pd((double*)&out_vec[i], output_vec);
	}

	// Handle any remaining elements
	for (; i < size; ++i) {
		out_vec[i].re += in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im += in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

RfReal32 vec_op_avx::fconv_dot_product(const RfReal32 *a, const RfReal32 *b, size_t size)
{
    __m256 acc = _mm256_setzero_ps();
        
	// Process 8 elements at a time
	size_t i = 0;
	for (; i + 8 <= size; i += 8) {
		__m256 va = _mm256_loadu_ps(a + i);
		__m256 vb = _mm256_loadu_ps(b + i);
		acc = _mm256_add_ps(acc, _mm256_mul_ps(va, vb));
	}
	
	// Horizontal sum across 8 lanes
	__m128 high = _mm256_extractf128_ps(acc, 1);
	__m128 low = _mm256_castps256_ps128(acc);
	__m128 sum = _mm_add_ps(high, low);
	sum = _mm_hadd_ps(sum, sum);
	sum = _mm_hadd_ps(sum, sum);
	RfReal32 result = _mm_cvtss_f32(sum);
	
	// Handle remaining elements
	for (; i < size; ++i) {
		result += a[i] * b[i];
	}
	
	return result;
}

RfReal64 vec_op_avx::dconv_dot_product(const RfReal64 *a, const RfReal64 *b, size_t size)
{
    __m256d acc = _mm256_setzero_pd();
        
	// Process 4 elements at a time
	size_t i = 0;
	for (; i + 4 <= size; i += 4) {
		__m256d va = _mm256_loadu_pd(a + i);
		__m256d vb = _mm256_loadu_pd(b + i);
		acc = _mm256_add_pd(acc, _mm256_mul_pd(va, vb));
	}
	
	// Horizontal sum across 4 lanes
	__m128d high = _mm256_extractf128_pd(acc, 1);
	__m128d low = _mm256_castpd256_pd128(acc);
	__m128d sum = _mm_add_pd(high, low);
	__m128d high2 = _mm_unpackhi_pd(sum, sum);
	sum = _mm_add_sd(sum, high2);
	RfReal64 result = _mm_cvtsd_f64(sum);
	
	// Handle remaining elements
	for (; i < size; ++i) {
		result += a[i] * b[i];
	}
	
	return result;
}

void vec_op_avx::fconv_direct_kernel(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count)
{
	fconv_partition(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count, kernel_len);
}

void vec_op_avx::dconv_direct_kernel(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count)
{
	dconv_partition(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count, kernel_len);
}

void vec_op_avx::fconv_partition(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count, RfInt32 partition_size)
{
	// AVX2-optimized partitioned convolution (8 floats at a time)
	for (RfInt32 i = 0; i < count; ++i) {
		__m256 acc = _mm256_setzero_ps();
		
		// Process kernel in chunks aligned with AVX width (8 floats)
		RfInt32 k = 0;
		for (; k + 8 <= kernel_len; k += 8) {
			// Load 8 kernel samples
			__m256 k_vec = _mm256_loadu_ps(kernel + k);
			
			// Load 8 history samples with circular buffer handling
			RfReal32 h_samples[8];
			for (RfInt32 j = 0; j < 8; ++j) {
				RfInt32 read_pos = (write_pos - 1 - (k + j) + hist_size) & hist_mask;
				h_samples[j] = history[read_pos];
			}
			__m256 h_vec = _mm256_loadu_ps(h_samples);
			
			acc = _mm256_add_ps(acc, _mm256_mul_ps(k_vec, h_vec));
		}
		
		// Horizontal sum
		__m128 high = _mm256_extractf128_ps(acc, 1);
		__m128 low = _mm256_castps256_ps128(acc);
		__m128 sum = _mm_add_ps(high, low);
		sum = _mm_hadd_ps(sum, sum);
		sum = _mm_hadd_ps(sum, sum);
		RfReal32 result = _mm_cvtss_f32(sum);
		
		// Handle remaining kernel samples
		for (; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			result += kernel[k] * history[read_pos];
		}
		
		output[i] = result;
		write_pos++;
	}
}

void vec_op_avx::dconv_partition(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count, RfInt32 partition_size)
{
	// AVX2-optimized partitioned convolution for doubles (4 at a time)
	for (RfInt32 i = 0; i < count; ++i) {
		__m256d acc = _mm256_setzero_pd();
		
		// Process kernel in chunks aligned with AVX width (4 doubles)
		RfInt32 k = 0;
		for (; k + 4 <= kernel_len; k += 4) {
			// Load 4 kernel samples
			__m256d k_vec = _mm256_loadu_pd(kernel + k);
			
			// Load 4 history samples with circular buffer handling
			RfReal64 h_samples[4];
			for (RfInt32 j = 0; j < 4; ++j) {
				RfInt32 read_pos = (write_pos - 1 - (k + j) + hist_size) & hist_mask;
				h_samples[j] = history[read_pos];
			}
			__m256d h_vec = _mm256_loadu_pd(h_samples);
			
			acc = _mm256_add_pd(acc, _mm256_mul_pd(k_vec, h_vec));
		}
		
		// Horizontal sum
		__m128d high = _mm256_extractf128_pd(acc, 1);
		__m128d low = _mm256_castpd256_pd128(acc);
		__m128d sum = _mm_add_pd(high, low);
		__m128d high2 = _mm_unpackhi_pd(sum, sum);
		sum = _mm_add_sd(sum, high2);
		RfReal64 result = _mm_cvtsd_f64(sum);
		
		// Handle remaining kernel samples
		for (; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			result += kernel[k] * history[read_pos];
		}
		
		output[i] = result;
		write_pos++;
	}
}
#endif

// ------------------------------------------------------
// NEON vector operator
#ifdef RF_NEON

// ------------------------------------------------------
// NEON FFT operations
void vec_op_neon::cfft_butterfly(RfCplx32* data, size_t n, size_t stride)
{
	for (size_t i = 0; i + 3 * stride < n; i += 4 * stride) {
		size_t idx0 = i;
		size_t idx2 = i + 2 * stride;

		float32x4_t a = vld1q_f32((const float32_t*)&data[idx0]);
		float32x4_t b = vld1q_f32((const float32_t*)&data[idx2]);

		// Butterfly: (a+b, a-b)
		float32x4_t sum = vaddq_f32(a, b);
		float32x4_t diff = vsubq_f32(a, b);

		vst1q_f32((float32_t*)&data[idx0], sum);
		vst1q_f32((float32_t*)&data[idx2], diff);
	}

	// Handle remaining elements
	for (size_t i = (n / (4 * stride)) * (4 * stride); i + stride < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		RfReal32 re_a = data[idx1].re;
		RfReal32 im_a = data[idx1].im;
		RfReal32 re_b = data[idx2].re;
		RfReal32 im_b = data[idx2].im;

		data[idx1].re = re_a + re_b;
		data[idx1].im = im_a + im_b;
		data[idx2].re = re_a - re_b;
		data[idx2].im = im_a - im_b;
	}
}

void vec_op_neon::cfft_butterfly(RfCplx64* data, size_t n, size_t stride)
{
#ifdef RF_ARM64
	for (size_t i = 0; i + stride < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		float64x2_t a = vld1q_f64((const float64_t*)&data[idx1]);
		float64x2_t b = vld1q_f64((const float64_t*)&data[idx2]);

		float64x2_t sum = vaddq_f64(a, b);
		float64x2_t diff = vsubq_f64(a, b);

		vst1q_f64((float64_t*)&data[idx1], sum);
		vst1q_f64((float64_t*)&data[idx2], diff);
	}
#else
	// ARM32 uses scalar fallback
	for (size_t i = 0; i + stride < n; i += 2 * stride) {
		size_t idx1 = i;
		size_t idx2 = i + stride;
		RfReal64 re_a = data[idx1].re;
		RfReal64 im_a = data[idx1].im;
		RfReal64 re_b = data[idx2].re;
		RfReal64 im_b = data[idx2].im;
		data[idx1].re = re_a + re_b;
		data[idx1].im = im_a + im_b;
		data[idx2].re = re_a - re_b;
		data[idx2].im = im_a - im_b;
	}
#endif
}

void vec_op_neon::cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle)
{
	static const uint32x4_t mask = { 0xFFFFFFFF, 0x0, 0xFFFFFFFF, 0x0 };

	// Load and duplicate twiddle real and imaginary parts
	float32x4_t tw = vdupq_n_f32(twiddle->re);
	float32x4_t tw_im = vdupq_n_f32(twiddle->im);

	// Interleave twiddle real and imaginary parts using vzipq_f32 (compatible with ARMv7/ARMv8)
	float32x4x2_t zipped = vzipq_f32(tw, tw_im);
	float32x2_t tw_low = vget_low_f32(zipped.val[0]);
	float32x2_t tw_high = vget_low_f32(zipped.val[1]);

	size_t i = 0;
	for (; i + 2 < n; i += 2) {
		float32x4_t d = vld1q_f32((const float32_t*)&data[i]);

		// Complex multiplication: (a+bi)(c+di) = (ac-bd) + (ad+bc)i
		float32x4_t mul_r = vmulq_f32(d, vcombine_f32(tw_low, tw_low));
		float32x4_t mul_i = vmulq_f32(d, vcombine_f32(tw_high, tw_high));

		float32x4_t d_swuf = vrev64q_f32(d);
		float32x4_t mul_i_swuf = vrev64q_f32(mul_i);

		float32x4_t result_r = vsubq_f32(mul_r, vrev64q_f32(mul_r));
		float32x4_t result_i = vaddq_f32(mul_i_swuf, vrev64q_f32(mul_i_swuf));

		float32x4_t result = vbslq_f32(mask, result_r, result_i);
		vst1q_f32((float32_t*)&data[i], result);
	}

	for (; i < n; ++i) {
		RfReal32 re = data[i].re * twiddle->re - data[i].im * twiddle->im;
		RfReal32 im = data[i].re * twiddle->im + data[i].im * twiddle->re;
		data[i].re = re;
		data[i].im = im;
	}
}

void vec_op_neon::cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle)
{
#ifdef RF_ARM64
	// Complex multiplication: (a+bi)(c+di) = (ac-bd) + (ad+bc)i
	// data[i] = (re, im), twiddle = (c, d)
	float64x2_t tw = vld1q_f64((const float64_t*)twiddle);
	// tw_swapped = (d, c)
	float64x2_t tw_swapped = vextq_f64(tw, tw, 1);
	
	for (size_t i = 0; i < n; ++i) {
		float64x2_t a = vld1q_f64((const float64_t*)&data[i]);  // a = (re, im)
		float64x2_t a_swapped = vextq_f64(a, a, 1);              // a_swapped = (im, re)

		// re_part = (re*c, im*d)
		float64x2_t re_part = vmulq_f64(a, tw);
		// im_part = (re*d, im*c)
		float64x2_t im_part = vmulq_f64(a, tw_swapped);

		// result_re = re*c - im*d
		float64_t result_re = vgetq_lane_f64(re_part, 0) - vgetq_lane_f64(re_part, 1);
		// result_im = re*d + im*c
		float64_t result_im = vgetq_lane_f64(im_part, 0) + vgetq_lane_f64(im_part, 1);

		float64x2_t result = { result_re, result_im };
		vst1q_f64((float64_t*)&data[i], result);
	}
#else
	// ARM32 scalar fallback
	for (size_t i = 0; i < n; ++i) {
		RfReal64 re = data[i].re * twiddle->re - data[i].im * twiddle->im;
		RfReal64 im = data[i].re * twiddle->im + data[i].im * twiddle->re;
		data[i].re = re;
		data[i].im = im;
	}
#endif
}

void vec_op_neon::cfft_bit_reverse(RfCplx32* data, size_t n)
{
	Helpers::cfft_bit_reverse(data, n);
}

void vec_op_neon::cfft_bit_reverse(RfCplx64* data, size_t n)
{
	Helpers::cfft_bit_reverse(data, n);
}

void vec_op_neon::cfft_scale(RfCplx32* data, size_t n, RfReal32 scale)
{
	float32x4_t scale_vec = vdupq_n_f32(scale);

	size_t i = 0;
	for (; i + 2 < n; i += 2) {
		float32x4_t d = vld1q_f32((const float32_t*)&data[i]);
		d = vmulq_f32(d, scale_vec);
		vst1q_f32((float32_t*)&data[i], d);
	}

	for (; i < n; ++i) {
		data[i].re *= scale;
		data[i].im *= scale;
	}
}

void vec_op_neon::cfft_scale(RfCplx64* data, size_t n, RfReal64 scale)
{
#ifdef RF_ARM64
	float64x2_t scale_vec = vdupq_n_f64(scale);

	for (size_t i = 0; i < n; ++i) {
		float64x2_t d = vld1q_f64((const float64_t*)&data[i]);
		d = vmulq_f64(d, scale_vec);
		vst1q_f64((float64_t*)&data[i], d);
	}
#else
	// ARM32 scalar fallback
	for (size_t i = 0; i < n; ++i) {
		data[i].re *= scale;
		data[i].im *= scale;
	}
#endif
}

// ============================================================
// Complete SIMD FFT implementations - NEON
// ============================================================

void vec_op_neon::cfvec_conj(RfCplx32* data, size_t n)
{
	// Mask: [1, -1, 1, -1] to negate imaginary parts
	const float32x4_t conj_mask = { 1.0f, -1.0f, 1.0f, -1.0f };
	
	size_t i = 0;
	for (; i + 2 <= n; i += 2) {
		float32x4_t d = vld1q_f32((const float32_t*)&data[i]);
		d = vmulq_f32(d, conj_mask);
		vst1q_f32((float32_t*)&data[i], d);
	}
	
	// Handle remaining element
	for (; i < n; ++i) {
		data[i].im = -data[i].im;
	}
}

void vec_op_neon::cfvec_conj(RfCplx64* data, size_t n)
{
#ifdef RF_ARM64
	// Mask: [1, -1] to negate imaginary part
	const float64x2_t conj_mask = { 1.0, -1.0 };
	
	for (size_t i = 0; i < n; ++i) {
		float64x2_t d = vld1q_f64((const float64_t*)&data[i]);
		d = vmulq_f64(d, conj_mask);
		vst1q_f64((float64_t*)&data[i], d);
	}
#else
	for (size_t i = 0; i < n; ++i) {
		data[i].im = -data[i].im;
	}
#endif
}

// NEON-optimized Cooley-Tukey FFT (float32)
void vec_op_neon::cfft_forward(RfCplx32* data, size_t n)
{
	if (n <= 1) return;
	
	// Bit-reversal permutation
	cfft_bit_reverse(data, n);
	
	// Compute log2(n)
	size_t log2n = 0;
	for (size_t temp = n; temp > 1; temp >>= 1) ++log2n;
	
	// Cooley-Tukey iterative FFT
	for (size_t s = 1; s <= log2n; ++s) {
		size_t m = size_t(1) << s;
		size_t half_m = m >> 1;
		
		RfReal32 angle_step = static_cast<RfReal32>(-TWOPI / m);
		
		for (size_t k = 0; k < n; k += m) {
			size_t j = 0;
			
			// Process pairs of butterflies with NEON (4 floats = 2 complex)
			for (; j + 1 < half_m; j += 2) {
				RfReal32 angle0 = angle_step * j;
				RfReal32 angle1 = angle_step * (j + 1);
				
				RfReal32 tw_re0 = std::cos(angle0), tw_im0 = std::sin(angle0);
				RfReal32 tw_re1 = std::cos(angle1), tw_im1 = std::sin(angle1);
				
				// Load even and odd elements
				float32x4_t even = vld1q_f32((const float32_t*)&data[k + j]);
				float32x4_t odd = vld1q_f32((const float32_t*)&data[k + j + half_m]);
				
				// Twiddle factors: [tw_re0, tw_im0, tw_re1, tw_im1]
				float32x4_t tw = { tw_re0, tw_im0, tw_re1, tw_im1 };
				
				// Complex multiplication: odd * twiddle
				// Extract real and imag parts using ARMv7/ARMv8 compatible intrinsics
				float32x4x2_t uzipped = vuzpq_f32(odd, odd);
				float32x4_t odd_re = uzipped.val[0];  // [re0, re1, re0, re1]
				float32x4_t odd_im = uzipped.val[1];  // [im0, im1, im0, im1]
				
				// Rearrange using vzipq_f32 (ARMv7/ARMv8 compatible)
				float32x4x2_t zipped_re = vzipq_f32(odd_re, odd_re);
				odd_re = zipped_re.val[0];  // [re0, re0, re1, re1]
				float32x4x2_t zipped_im = vzipq_f32(odd_im, odd_im);
				odd_im = zipped_im.val[0];  // [im0, im0, im1, im1]
				
				// tw_swapped: [tw_im0, tw_re0, tw_im1, tw_re1]
				float32x4_t tw_swap = vrev64q_f32(tw);
				
				// prod1 = odd_re * tw = [re0*tw_re0, re0*tw_im0, re1*tw_re1, re1*tw_im1]
				float32x4_t prod1 = vmulq_f32(odd_re, tw);
				// prod2 = odd_im * tw_swap = [im0*tw_im0, im0*tw_re0, im1*tw_im1, im1*tw_re1]
				float32x4_t prod2 = vmulq_f32(odd_im, tw_swap);
				
				// t = [re0*tw_re0 - im0*tw_im0, re0*tw_im0 + im0*tw_re0, ...]
				// NEON doesn't have addsub, so we use manual approach
				// For even indices (0, 2): subtract
				// For odd indices (1, 3): add
				float32x4_t neg_mask = { 1.0f, -1.0f, 1.0f, -1.0f };
				float32x4_t t = vmlaq_f32(prod1, prod2, neg_mask);
				// Correction: we need sub for real, add for imag
				// t = prod1 +/- prod2 with alternating signs
				// Actually: t_re = prod1[0] - prod2[0], t_im = prod1[1] + prod2[1]
				// Use vfmsq for subtract and vfmaq for add
				float32x4_t sub_result = vsubq_f32(prod1, prod2);  // [re*tw_re - im*tw_im, re*tw_im - im*tw_re, ...]
				float32x4_t add_result = vaddq_f32(prod1, prod2);  // [re*tw_re + im*tw_im, re*tw_im + im*tw_re, ...]
				
				// Select: real from sub, imag from add
				// Use vtrnq_f32 (ARMv7/ARMv8 compatible)
				float32x4x2_t trn = vtrnq_f32(sub_result, add_result);
				t = trn.val[0];  // interleaved: [sub[0], add[0], sub[2], add[2]] -> real parts
				
				// Actually simpler approach: scalar complex multiplication is safer
				// Let's use a cleaner NEON pattern
				// Recalculate properly:
				float32_t t_re0 = tw_re0 * vgetq_lane_f32(odd, 0) - tw_im0 * vgetq_lane_f32(odd, 1);
				float32_t t_im0 = tw_im0 * vgetq_lane_f32(odd, 0) + tw_re0 * vgetq_lane_f32(odd, 1);
				float32_t t_re1 = tw_re1 * vgetq_lane_f32(odd, 2) - tw_im1 * vgetq_lane_f32(odd, 3);
				float32_t t_im1 = tw_im1 * vgetq_lane_f32(odd, 2) + tw_re1 * vgetq_lane_f32(odd, 3);
				
				float32x4_t t_vec = { t_re0, t_im0, t_re1, t_im1 };
				
				// Butterfly
				float32x4_t sum = vaddq_f32(even, t_vec);
				float32x4_t diff = vsubq_f32(even, t_vec);
				
				vst1q_f32((float32_t*)&data[k + j], sum);
				vst1q_f32((float32_t*)&data[k + j + half_m], diff);
			}
			
			// Handle remaining butterfly (scalar)
			for (; j < half_m; ++j) {
				RfReal32 angle = angle_step * j;
				RfReal32 tw_re = std::cos(angle);
				RfReal32 tw_im = std::sin(angle);
				
				size_t even_idx = k + j;
				size_t odd_idx = k + j + half_m;
				
				RfReal32 t_re = data[odd_idx].re * tw_re - data[odd_idx].im * tw_im;
				RfReal32 t_im = data[odd_idx].re * tw_im + data[odd_idx].im * tw_re;
				
				data[odd_idx].re = data[even_idx].re - t_re;
				data[odd_idx].im = data[even_idx].im - t_im;
				data[even_idx].re += t_re;
				data[even_idx].im += t_im;
			}
		}
	}
}

void vec_op_neon::cfft_forward(RfCplx64* data, size_t n)
{
#ifdef RF_ARM64
	if (n <= 1) return;
	
	// Bit-reversal permutation
	cfft_bit_reverse(data, n);
	
	// Compute log2(n)
	size_t log2n = 0;
	for (size_t temp = n; temp > 1; temp >>= 1) ++log2n;
	
	// Cooley-Tukey iterative FFT
	for (size_t s = 1; s <= log2n; ++s) {
		size_t m = size_t(1) << s;
		size_t half_m = m >> 1;
		
		RfReal64 angle_step = -TWOPI / m;
		
		for (size_t k = 0; k < n; k += m) {
			for (size_t j = 0; j < half_m; ++j) {
				RfReal64 angle = angle_step * j;
				RfReal64 tw_re = std::cos(angle);
				RfReal64 tw_im = std::sin(angle);
				
				size_t even_idx = k + j;
				size_t odd_idx = k + j + half_m;
				
				// Load odd element
				float64x2_t odd = vld1q_f64((const float64_t*)&data[odd_idx]);
				
				// Complex multiplication using NEON
				// t = odd * twiddle = (re*tw_re - im*tw_im, re*tw_im + im*tw_re)
				float64_t odd_re = vgetq_lane_f64(odd, 0);
				float64_t odd_im = vgetq_lane_f64(odd, 1);
				
				float64_t t_re = odd_re * tw_re - odd_im * tw_im;
				float64_t t_im = odd_re * tw_im + odd_im * tw_re;
				
				float64x2_t t = { t_re, t_im };
				
				// Butterfly
				float64x2_t even_v = vld1q_f64((const float64_t*)&data[even_idx]);
				float64x2_t sum = vaddq_f64(even_v, t);
				float64x2_t diff = vsubq_f64(even_v, t);
				
				vst1q_f64((float64_t*)&data[even_idx], sum);
				vst1q_f64((float64_t*)&data[odd_idx], diff);
			}
		}
	}
#else
	// Fallback to scalar implementation for non-ARM64
	if (n <= 1) return;
	cfft_bit_reverse(data, n);
	
	size_t log2n = 0;
	for (size_t temp = n; temp > 1; temp >>= 1) ++log2n;
	
	for (size_t s = 1; s <= log2n; ++s) {
		size_t m = size_t(1) << s;
		size_t half_m = m >> 1;
		RfReal64 angle_step = -TWOPI / m;
		
		for (size_t k = 0; k < n; k += m) {
			for (size_t j = 0; j < half_m; ++j) {
				RfReal64 angle = angle_step * j;
				RfReal64 tw_re = std::cos(angle);
				RfReal64 tw_im = std::sin(angle);
				
				size_t even_idx = k + j;
				size_t odd_idx = k + j + half_m;
				
				RfReal64 t_re = data[odd_idx].re * tw_re - data[odd_idx].im * tw_im;
				RfReal64 t_im = data[odd_idx].re * tw_im + data[odd_idx].im * tw_re;
				
				data[odd_idx].re = data[even_idx].re - t_re;
				data[odd_idx].im = data[even_idx].im - t_im;
				data[even_idx].re += t_re;
				data[even_idx].im += t_im;
			}
		}
	}
#endif
}

void vec_op_neon::cfft_inverse(RfCplx32* data, size_t n)
{
	if (n <= 1) return;
	
	cfvec_conj(data, n);
	cfft_forward(data, n);
	cfvec_conj(data, n);
	cfft_scale(data, n, 1.0f / static_cast<RfReal32>(n));
}

void vec_op_neon::cfft_inverse(RfCplx64* data, size_t n)
{
	if (n <= 1) return;
	
	cfvec_conj(data, n);
	cfft_forward(data, n);
	cfvec_conj(data, n);
	cfft_scale(data, n, 1.0 / static_cast<RfReal64>(n));
}

void vec_op_neon::fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;
	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(in_vec1 + i); // Load 4 floats from in_vec1
		float32x4_t y_4 = vld1q_f32(in_vec2 + i); // Load 4 floats from in_vec2
		float32x4_t z_4 = vaddq_f32(x_4, y_4);    // Add corresponding elements
		vst1q_f32(out_vec + i, z_4);              // Store the result in out_vec
	}

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + in_vec2[i]; // Handle the remaining elements using scalar addition
	}

}

void vec_op_neon::fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(in_vec1 + i); // Load 4 floats from in_vec1
		float32x4_t y_4 = vld1q_f32(in_vec2 + i); // Load 4 floats from in_vec2
		float32x4_t z_4 = vmulq_f32(x_4, y_4);    // Multiply corresponding elements
		vst1q_f32(out_vec + i, z_4);              // Store the result in out_vec
	}

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * in_vec2[i]; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_neon::fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	float32x4_t add_v = vdupq_n_f32(add); // Broadcast add to all elements of a NEON register

	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(in_vec1 + i); // Load 4 floats from in_vec1
		float32x4_t y_4 = vaddq_f32(x_4, add_v);   // Add add_v to each element of x_4
		vst1q_f32(out_vec + i, y_4);              // Store the result in out_vec
	}

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + add; // Handle the remaining elements using scalar addition
	}
}

void vec_op_neon::fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	float32x4_t mul_v = vdupq_n_f32(mul); // Broadcast mul to all elements of a NEON register

	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(in_vec1 + i); // Load 4 floats from in_vec1
		float32x4_t y_4 = vmulq_f32(x_4, mul_v);   // Multiply each element of x_4 with mul_v
		vst1q_f32(out_vec + i, y_4);              // Store the result in out_vec
	}

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * mul; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_neon::fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;
	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(in_vec1 + i); // Load 4 floats from in_vec1
		float32x4_t y_4 = vld1q_f32(in_vec2 + i); // Load 4 floats from in_vec2
		float32x4_t z_4 = vsubq_f32(x_4, y_4);    // Add corresponding elements
		vst1q_f32(out_vec + i, z_4);              // Store the result in out_vec
	}

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] - in_vec2[i]; // Handle the remaining elements using scalar addition
	}
}

void vec_op_neon::fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	float32x4_t add_v = vdupq_n_f32(sub); // Broadcast add to all elements of a NEON register

	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(in_vec1 + i); // Load 4 floats from in_vec1
		float32x4_t y_4 = vsubq_f32(x_4, add_v);   // Add add_v to each element of x_4
		vst1q_f32(out_vec + i, y_4);              // Store the result in out_vec
	}

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] - sub; // Handle the remaining elements using scalar addition
	}
}

void vec_op_neon::fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(in_vec1 + i); // Load 4 floats from in_vec1
		float32x4_t y_4 = vld1q_f32(in_vec2 + i); // Load 4 floats from in_vec2
#ifdef RF_ARM64 // v7a doesn't support vdivq_f32 instruction. 
		float32x4_t z_4 = vdivq_f32(x_4, y_4);    // Divide corresponding elements
#else
		// Compute the reciprocal approximation
		float32x4_t recip_approx = vrecpeq_f32(y_4);

		// Refine the approximation
		recip_approx = vmulq_f32(vrecpsq_f32(y_4, recip_approx), recip_approx);
		recip_approx = vmulq_f32(vrecpsq_f32(y_4, recip_approx), recip_approx);

		// Perform the division
		float32x4_t z_4 = vmulq_f32(x_4, recip_approx);
#endif

		vst1q_f32(out_vec + i, z_4);              // Store the result in out_vec
	}

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] / in_vec2[i]; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_neon::fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	float32x4_t div_v = vdupq_n_f32(1.0f / div); // Broadcast mul to all elements of a NEON register

	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(in_vec1 + i); // Load 4 floats from in_vec1
		float32x4_t y_4 = vmulq_f32(x_4, div_v);   // Divide each element of x_4 with mul_v
		vst1q_f32(out_vec + i, y_4);              // Store the result in out_vec
	}

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] / div; // Handle the remaining elements using scalar multiplication
	}
}

void vec_op_neon::fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	size_t neon_size = size - size % 4;
	float32x4_t sum = vdupq_n_f32(0.0f);  // Initialize the sum register to 0

	for (int i = 0; i < neon_size; i += 4) {
		// Load 4 floats into a NEON register
		float32x4_t values = vld1q_f32(&in_vec[i]);

		// Add to the running sum
		sum = vaddq_f32(sum, values);
	}

	// Horizontal sum of the NEON register
	float32x2_t sumPair = vadd_f32(vget_low_f32(sum), vget_high_f32(sum));
	*out = vget_lane_f32(vpadd_f32(sumPair, sumPair), 0);

	// Remaining vectors using scaler add
	for (int i = neon_size; i < size; ++i) {
		*out += in_vec[i];
	}
}

void vec_op_neon::fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	size_t neon_size = size - size % 4;
	float32x4_t sum = vdupq_n_f32(0.0f);  // Initialize the sum register to 0

	for (int i = 0; i < neon_size; i += 4) {
		// Load 4 floats into a NEON register
		float32x4_t values = vld1q_f32(&in_vec[i]);

		// Compute the absolute values
		values = vabsq_f32(values);

		// Add to the running sum
		sum = vaddq_f32(sum, values);
	}

	// Horizontal sum of the NEON register
	float32x2_t sumPair = vadd_f32(vget_low_f32(sum), vget_high_f32(sum));
	*out = vget_lane_f32(vpadd_f32(sumPair, sumPair), 0);

	// Remaining vectors using scaler add
	for (int i = neon_size; i < size; ++i) {
		*out += in_vec[i];
	}
}

void vec_op_neon::fvec_copy(const RfReal32* src, RfReal32* dst, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		float32x4_t x_4 = vld1q_f32(src + i);
		vst1q_f32(dst + i, x_4);
	}

	for (i = neon_size; i < size; ++i) {
		dst[i] = src[i];
	}
}

void vec_op_neon::fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	float32x4_t val_v = vdupq_n_f32(*value);

	size_mod = size % 4;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 4) {
		vst1q_f32(dst + i, val_v);
	}

	for (i = neon_size; i < size; ++i) {
		dst[i] = *value;
	}
}

void vec_op_neon::dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

#ifdef RF_ARM64

	size_mod = size % 2;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 2) {
		float64x2_t x_4 = vld1q_f64(in_vec1 + i); // Load 2 doubles from in_vec1
		float64x2_t y_4 = vld1q_f64(in_vec2 + i); // Load 2 doubles from in_vec2
		float64x2_t z_4 = vaddq_f64(x_4, y_4);    // Add corresponding elements
		vst1q_f64(out_vec + i, z_4);              // Store the result in out_vec
	}

#else

	neon_size = 0;

#endif

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + in_vec2[i];
	}
}

void vec_op_neon::dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

#ifdef RF_ARM64

	size_mod = size % 2;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 2) {
		float64x2_t x_4 = vld1q_f64(in_vec1 + i); // Load 2 doubles from in_vec1
		float64x2_t y_4 = vld1q_f64(in_vec2 + i); // Load 2 doubles from in_vec2
		float64x2_t z_4 = vmulq_f64(x_4, y_4);    // Add corresponding elements
		vst1q_f64(out_vec + i, z_4);              // Store the result in out_vec
}

#else

	neon_size = 0;

#endif

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * in_vec2[i];
	}
}

void vec_op_neon::dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

#ifdef RF_ARM64

	float64x2_t add_v = vdupq_n_f64(add); // Broadcast add to all elements of a NEON register

	size_mod = size % 2;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 2) {
		float64x2_t x_4 = vld1q_f64(in_vec1 + i); // Load 4 floats from in_vec1
		float64x2_t y_4 = vaddq_f64(x_4, add_v);   // Add add_v to each element of x_4
		vst1q_f64(out_vec + i, y_4);              // Store the result in out_vec
	}

#else

	neon_size = 0;

#endif

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] + add;
	}
}

void vec_op_neon::dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

#ifdef RF_ARM64

	float64x2_t mul_v = vdupq_n_f64(mul); // Broadcast add to all elements of a NEON register

	size_mod = size % 2;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 2) {
		float64x2_t x_4 = vld1q_f64(in_vec1 + i); // Load 4 floats from in_vec1
		float64x2_t y_4 = vmulq_f64(x_4, mul_v);   // Add add_v to each element of x_4
		vst1q_f64(out_vec + i, y_4);              // Store the result in out_vec
	}

#else

	neon_size = 0;

#endif

	for (i = neon_size; i < size; ++i) {
		out_vec[i] = in_vec1[i] * mul;
	}
}

void vec_op_neon::dvec_copy(const RfReal64* src, RfReal64* dst, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

#ifdef RF_ARM64

	size_mod = size % 2;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 2) {
		float64x2_t x_4 = vld1q_f64(src + i); // Load 2 doubles from src
		vst1q_f64(dst + i, x_4);             // Store directly to dst
	}

#else

	neon_size = 0;

#endif

	for (i = neon_size; i < size; ++i) {
		dst[i] = src[i];
	}
}

void vec_op_neon::cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size)
{
	size_t i;
	size_t size_mod;
	size_t neon_size;

	static const uint32x4_t mask = { 0xFFFFFFFF, 0x0, 0xFFFFFFFF, 0x0 };

	size_mod = size % 2;
	neon_size = size - size_mod;

 	for (i = 0; i < neon_size; i += 2) { // Process two complex numbers at a time

		float32x4_t _vec1 = vld1q_f32((const float32_t*)&in_vec1[i]);	// [x0, x1, x2, x3]
		float32x4_t _vec2 = vld1q_f32((const float32_t*)&in_vec2[i]);	// [y0, y1, y2, y3]

		float32x4_t _vec2_pmtd = vrev64q_f32(_vec2);		// [y1, y0, y3, y2]

		float32x4_t mul_r = vmulq_f32(_vec1, _vec2);		// [x0y0, x1y1, x2y2, x3y3]
		float32x4_t mul_i = vmulq_f32(_vec1, _vec2_pmtd);	// [x0y1, x1y0, x2y3, x3y2]

		float32x4_t mul_r_shuff = vrev64q_f32(mul_r);		// [x1y1, x0y0, x3y3, x2y2]
		float32x4_t mul_i_shuff = vrev64q_f32(mul_i);		// [x1y0, x0y1, x3y2, x2y3]

		// Real part lanes 0,2: x0y0-x1y1 / x2y2-x3y3 (lanes 1,3 are garbage but masked out below).
		float32x4_t result_r = vsubq_f32(mul_r, mul_r_shuff);
		// Imag part lanes 1,3: x1y0+x0y1 / x3y2+x2y3 (lanes 0,2 are also valid because + is commutative).
		float32x4_t result_i = vaddq_f32(mul_i, mul_i_shuff);

		// Bug fix: the original code blended mul_r/mul_i (raw products) instead of
		// result_r/result_i (after add/sub), so the imaginary part of each complex
		// product was never assembled correctly on NEON. This produced wrong values
		// in cfvec_mlt / cfvec_mlt_add, which are the core of partitioned complex
		// convolution and showed up as audible glitches/noise on Android.
		float32x4_t result = vbslq_f32(mask, result_r, result_i);
		vst1q_f32((float32_t*)&out_vec[i], result);
	}

	// Handle any remaining elements
	for (i = neon_size; i < size; ++i) {
		out_vec[i].re = in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im = in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

void vec_op_neon::cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size)
{
	while (size) {
		out_vec->re = in_vec1->re * in_vec2->re - in_vec1->im * in_vec2->im;
		out_vec->im = in_vec1->re * in_vec2->im + in_vec1->im * in_vec2->re;
		out_vec++;
		in_vec1++;
		in_vec2++;
		size--;
	}
}

void vec_op_neon::cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) {
	size_t i;
	size_t size_mod;
	size_t neon_size;
	static const uint32x4_t mask = { 0xFFFFFFFF, 0x0, 0xFFFFFFFF, 0x0 };

	size_mod = size % 2;
	neon_size = size - size_mod;

	for (i = 0; i < neon_size; i += 2) { // Process two complex numbers at a time

		float32x4_t _vec1 = vld1q_f32((const float32_t*)&in_vec1[i]);	// [x0, x1, x2, x3]
		float32x4_t _vec2 = vld1q_f32((const float32_t*)&in_vec2[i]);	// [y0, y1, y2, y3]

		float32x4_t _vec2_pmtd = vrev64q_f32(_vec2);		// [y1, y0, y3, y2]

		float32x4_t mul_r = vmulq_f32(_vec1, _vec2);		// [x0y0, x1y1, x2y2, x3y3]
		float32x4_t mul_i = vmulq_f32(_vec1, _vec2_pmtd);	// [x0y1, x1y0, x2y3, x3y2]

		float32x4_t mul_r_shuff = vrev64q_f32(mul_r);		// [x1y1, x0y0, x3y3, x2y2]
		float32x4_t mul_i_shuff = vrev64q_f32(mul_i);		// [x1y0, x0y1, x3y2, x2y3]

		float32x4_t result_r = vsubq_f32(mul_r, mul_r_shuff);
		float32x4_t result_i = vaddq_f32(mul_i, mul_i_shuff);

		// Bug fix: see comment in cfvec_mlt above. The original blended raw
		// products (mul_r/mul_i) instead of the post add/sub results, so the
		// accumulated complex product was wrong on NEON.
		float32x4_t result = vbslq_f32(mask, result_r, result_i);
		float32x4_t out = vld1q_f32((const float32_t*)&out_vec[i]);
		out = vaddq_f32(result, out);

		vst1q_f32((float32_t*)&out_vec[i], out);
	}

	// Handle any remaining elements
	for (i = neon_size; i < size; ++i) {
		out_vec[i].re += in_vec1[i].re * in_vec2[i].re - in_vec1[i].im * in_vec2[i].im;
		out_vec[i].im += in_vec1[i].re * in_vec2[i].im + in_vec1[i].im * in_vec2[i].re;
	}
}

void vec_op_neon::cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) {

	while (size) {
		out_vec->re += in_vec1->re * in_vec2->re - in_vec1->im * in_vec2->im;
		out_vec->im += in_vec1->re * in_vec2->im + in_vec1->im * in_vec2->re;
		out_vec++;
		in_vec1++;
		in_vec2++;
		size--;
	}

}

RfReal32 vec_op_neon::fconv_dot_product(const RfReal32 *a, const RfReal32 *b, size_t size)
{
    float32x4_t acc = vdupq_n_f32(0.0f);
        
	// Process 4 elements at a time
	size_t i = 0;
	for (; i + 4 <= size; i += 4) {
		float32x4_t va = vld1q_f32(a + i);
		float32x4_t vb = vld1q_f32(b + i);
		acc = vmlaq_f32(acc, va, vb);
	}
	
	// Horizontal sum
	float32x2_t sum = vadd_f32(vget_low_f32(acc), vget_high_f32(acc));
	sum = vpadd_f32(sum, sum);
	RfReal32 result = vget_lane_f32(sum, 0);
	
	// Handle remaining elements
	for (; i < size; ++i) {
		result += a[i] * b[i];
	}
	
	return result;
}

RfReal64 vec_op_neon::dconv_dot_product(const RfReal64 *a, const RfReal64 *b, size_t size)
{
#ifdef RF_ARM64
    float64x2_t acc = vdupq_n_f64(0.0);
        
	// Process 2 elements at a time
	size_t i = 0;
	for (; i + 2 <= size; i += 2) {
		float64x2_t va = vld1q_f64(a + i);
		float64x2_t vb = vld1q_f64(b + i);
		acc = vmlaq_f64(acc, va, vb);
	}
	
	// Horizontal sum
	RfReal64 result = vgetq_lane_f64(acc, 0) + vgetq_lane_f64(acc, 1);
	
	// Handle remaining elements
	for (; i < size; ++i) {
		result += a[i] * b[i];
	}
	
	return result;
#else
	// ARM32 scalar fallback
	RfReal64 result = 0.0;
	for (size_t i = 0; i < size; ++i) {
		result += a[i] * b[i];
	}
	return result;
#endif
}

void vec_op_neon::fconv_direct_kernel(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count)
{
	fconv_partition(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count, kernel_len);
}

void vec_op_neon::dconv_direct_kernel(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count)
{
	dconv_partition(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count, kernel_len);
}

void vec_op_neon::fconv_partition(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count, RfInt32 partition_size)
{
	// NEON-optimized partitioned convolution (4 floats at a time)
	for (RfInt32 i = 0; i < count; ++i) {
		float32x4_t acc = vdupq_n_f32(0.0f);
		
		RfInt32 k = 0;
		for (; k + 4 <= kernel_len; k += 4) {
			// Load 4 kernel samples
			float32x4_t k_vec = vld1q_f32(kernel + k);
			
			// Load 4 history samples with circular buffer handling
			RfReal32 h_samples[4];
			for (RfInt32 j = 0; j < 4; ++j) {
				RfInt32 read_pos = (write_pos - 1 - (k + j) + hist_size) & hist_mask;
				h_samples[j] = history[read_pos];
			}
			float32x4_t h_vec = vld1q_f32(h_samples);
			
			acc = vmlaq_f32(acc, k_vec, h_vec);
		}
		
		// Horizontal sum
		float32x2_t sum = vadd_f32(vget_low_f32(acc), vget_high_f32(acc));
		sum = vpadd_f32(sum, sum);
		RfReal32 result = vget_lane_f32(sum, 0);
		
		// Handle remaining kernel samples
		for (; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			result += kernel[k] * history[read_pos];
		}
		
		output[i] = result;
		write_pos++;
	}
}

void vec_op_neon::dconv_partition(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count, RfInt32 partition_size)
{
#ifdef RF_ARM64
	// NEON-optimized partitioned convolution for doubles (2 at a time)
	for (RfInt32 i = 0; i < count; ++i) {
		float64x2_t acc = vdupq_n_f64(0.0);
		
		RfInt32 k = 0;
		for (; k + 2 <= kernel_len; k += 2) {
			// Load 2 kernel samples
			float64x2_t k_vec = vld1q_f64(kernel + k);
			
			// Load 2 history samples with circular buffer handling
			RfReal64 h_samples[2];
			for (RfInt32 j = 0; j < 2; ++j) {
				RfInt32 read_pos = (write_pos - 1 - (k + j) + hist_size) & hist_mask;
				h_samples[j] = history[read_pos];
			}
			float64x2_t h_vec = vld1q_f64(h_samples);
			
			acc = vmlaq_f64(acc, k_vec, h_vec);
		}
		
		// Horizontal sum
		RfReal64 result = vgetq_lane_f64(acc, 0) + vgetq_lane_f64(acc, 1);
		
		// Handle remaining kernel samples
		for (; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			result += kernel[k] * history[read_pos];
		}
		
		output[i] = result;
		write_pos++;
	}
#else
	// ARM32 scalar fallback
	for (RfInt32 i = 0; i < count; ++i) {
		RfReal64 result = 0.0;
		for (RfInt32 k = 0; k < kernel_len; ++k) {
			RfInt32 read_pos = (write_pos - 1 - k + hist_size) & hist_mask;
			result += kernel[k] * history[read_pos];
		}
		output[i] = result;
		write_pos++;
	}
#endif
}

#endif // RF_NEON

// ------------------------------------------------------
// Fast vector operators entry

void RfRapidVec::select_platform()
{
	int cpuFlags = RfCpuId::InitCpuFlags();

#ifdef RF_SSE
#ifdef RF_DETECT_AVX2
	if(cpuFlags & RfCpuId::kCpuHasAVX2)
		_op = std::make_shared<vec_op_avx>();
	else 
#endif
		if (cpuFlags & RfCpuId::kCpuHasSSE2)
			_op = std::make_shared<vec_op_sse>();
		else
#elif defined(RF_NEON)
	if (cpuFlags & RfCpuId::kCpuHasNEON)
		_op = std::make_shared<vec_op_neon>();
	else
#endif
		_op = std::make_shared<vec_op_default>();
}

void RfRapidVec::init()
{
	// FTZ/DAZ lives in thread-local CPU state, so we need to re-apply it on
	// every calling thread rather than only during the first global init.
	DenormalGuard::enableFlushToZero();

	static std::once_flag s_platform_init_once;
	std::call_once(s_platform_init_once, []() {
		select_platform();
		const int cpuFlags = RfCpuId::InitCpuFlags();
		const char* simd_path =
#if defined(RF_DETECT_AVX2)
			(cpuFlags & RfCpuId::kCpuHasAVX2) ? "avx2+fma3" :
			(cpuFlags & RfCpuId::kCpuHasSSE2) ? "sse2" :
#elif defined(RF_SSE)
			(cpuFlags & RfCpuId::kCpuHasSSE2) ? "sse2" :
#elif defined(RF_NEON)
			(cpuFlags & RfCpuId::kCpuHasNEON) ? "neon" :
#endif
			"scalar";
		const bool has_fma3 = (cpuFlags & RfCpuId::kCpuHasFMA3) != 0;
		DBG("RfRapidVec init: simd_path=" << simd_path
			<< " fma3=" << (has_fma3 ? "true" : "false")
			<< " avx2=" << ((cpuFlags & RfCpuId::kCpuHasAVX2) ? "true" : "false")
			<< " sse2=" << ((cpuFlags & RfCpuId::kCpuHasSSE2) ? "true" : "false"));
	});
}

bool RfRapidVec::isDenormalHandlingEnabled()
{
	return DenormalGuard::isFlushToZeroEnabled();
}

void RfRapidVec::fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	_op->fvec_add(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	_op->fvec_mlt(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size)
{
	_op->fvec_add(in_vec1, add, out_vec, size);
}

void RfRapidVec::fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size)
{
	_op->fvec_mlt(in_vec1, mul, out_vec, size);
}

void RfRapidVec::fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	_op->fvec_sub(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size)
{
	_op->fvec_sub(in_vec1, sub, out_vec, size);
}

void RfRapidVec::fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size)
{
	_op->fvec_div(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size)
{
	_op->fvec_div(in_vec1, div, out_vec, size);
}

void RfRapidVec::fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	_op->fvec_sum(in_vec, out, size);
}

void RfRapidVec::fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size)
{
	_op->fvec_abssum(in_vec, out, size);
}

void RfRapidVec::fvec_copy(const RfReal32* src, RfReal32* dst, size_t size)
{
	_op->fvec_copy(src, dst, size);
}

void RfRapidVec::fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size)
{
	_op->fvec_set_scalar(value, dst, size);
}

void RfRapidVec::dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	_op->dvec_add(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size)
{
	_op->dvec_mlt(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size)
{
	_op->dvec_add(in_vec1, add, out_vec, size);
}

void RfRapidVec::dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size)
{
	_op->dvec_mlt(in_vec1, mul, out_vec, size);
}

void RfRapidVec::dvec_copy(const RfReal64* src, RfReal64* dst, size_t size)
{
	_op->dvec_copy(src, dst, size);
}

void RfRapidVec::cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size)
{
	_op->cfvec_mlt(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size)
{
	_op->cdvec_mlt(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size)
{
	_op->cfvec_mlt_add(in_vec1, in_vec2, out_vec, size);
}

void RfRapidVec::cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size)
{
	_op->cdvec_mlt_add(in_vec1, in_vec2, out_vec, size);
}

RfReal32 RF::RfRapidVec::fconv_dot_product(const RfReal32 *a, const RfReal32 *b, size_t size)
{
    return _op->fconv_dot_product(a, b, size);
}

RfReal64 RF::RfRapidVec::dconv_dot_product(const RfReal64 *a, const RfReal64 *b, size_t size)
{
    return _op->dconv_dot_product(a, b, size);
}

void RF::RfRapidVec::fconv_direct_kernel(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count)
{
	_op->fconv_direct_kernel(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count);
}

void RF::RfRapidVec::dconv_direct_kernel(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count)
{
	_op->dconv_direct_kernel(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count);
}

void RF::RfRapidVec::fconv_partition(const RfReal32 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal32 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal32 *output, RfInt32 count, RfInt32 partition_size)
{
	_op->fconv_partition(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count, partition_size);
}

void RF::RfRapidVec::dconv_partition(const RfReal64 *history, RfInt32 hist_size, RfInt32 hist_mask, const RfReal64 *kernel, RfInt32 kernel_len, RfInt32 write_pos, RfReal64 *output, RfInt32 count, RfInt32 partition_size)
{
	_op->dconv_partition(history, hist_size, hist_mask, kernel, kernel_len, write_pos, output, count, partition_size);
}

void RF::RfRapidVec::cfft_butterfly(RfCplx32* data, size_t n, size_t stride)
{
	_op->cfft_butterfly(data, n, stride);
}

void RF::RfRapidVec::cfft_butterfly(RfCplx64* data, size_t n, size_t stride)
{
	_op->cfft_butterfly(data, n, stride);
}

void RF::RfRapidVec::cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle)
{
	_op->cfft_twiddle_mul(data, n, twiddle);
}

void RF::RfRapidVec::cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle)
{
	_op->cfft_twiddle_mul(data, n, twiddle);
}

void RF::RfRapidVec::cfft_bit_reverse(RfCplx32* data, size_t n)
{
	_op->cfft_bit_reverse(data, n);
}

void RF::RfRapidVec::cfft_bit_reverse(RfCplx64* data, size_t n)
{
	_op->cfft_bit_reverse(data, n);
}

void RF::RfRapidVec::cfft_scale(RfCplx32* data, size_t n, RfReal32 scale)
{
	_op->cfft_scale(data, n, scale);
}

void RF::RfRapidVec::cfft_scale(RfCplx64* data, size_t n, RfReal64 scale)
{
	_op->cfft_scale(data, n, scale);
}

void RF::RfRapidVec::cfft_forward(RfCplx32* data, size_t n)
{
	_op->cfft_forward(data, n);
}

void RF::RfRapidVec::cfft_forward(RfCplx64* data, size_t n)
{
	_op->cfft_forward(data, n);
}

void RF::RfRapidVec::cfft_inverse(RfCplx32* data, size_t n)
{
	_op->cfft_inverse(data, n);
}

void RF::RfRapidVec::cfft_inverse(RfCplx64* data, size_t n)
{
	_op->cfft_inverse(data, n);
}

void RF::RfRapidVec::cfvec_conj(RfCplx32* data, size_t n)
{
	_op->cfvec_conj(data, n);
}

void RF::RfRapidVec::cfvec_conj(RfCplx64* data, size_t n)
{
	_op->cfvec_conj(data, n);
}


// -----------------------------------------------------------------
// For testing use only!!!!

void RfRapidVec::force_default()
{
	_op = std::make_shared<vec_op_default>();
}

void RfRapidVec::force_sse()
{
	_op = std::make_shared<vec_op_sse>();
}

void RfRapidVec::force_avx()
{
	_op = std::make_shared<vec_op_avx>();
}

void RfRapidVec::force_neon()
{
	_op = std::make_shared<vec_op_neon>();
}

#endif

