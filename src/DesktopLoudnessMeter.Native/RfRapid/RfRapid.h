/* ================================================================================
* Rayforce DSP Operation Accelerator Library.
* Including vectorization, matrix operations, etc.
* 
* We hereby show sincere respect to the marvellous work by Wang Zhe
* who generously give an aid on this topic.   |_ Orz
================================================================================ */

#ifndef _RF_RAPID_
#define _RF_RAPID_

#include <cstddef>
#include <memory>

#include "RfNumeralTypes.h"
#include "RfMacros.h"
#include "RfCPUID.h"

// ================================================================================
// Platform switch
#if defined(RF_X86)

#define RF_SSE
#ifndef RF_MOBILE
#define RF_DETECT_AVX2	// Better use AVX2 instruction set. 
#endif

#elif defined(RF_ARM)

#ifndef RF_NEON

#if defined(RF_ARM64)
# define RF_NEON
#elif defined(__ARM_NEON__)
# define RF_NEON
#endif

#endif

#endif

namespace RF {
	// ================================================================================
	// Fast vector operators

	/// <summary>
	/// Base class for vector operations. 
	/// </summary>
	class vec_op_base {
	public:

		// Float data vector operations
		virtual void fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) {};
		virtual void fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size) {};
		virtual void fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) {};
		virtual void fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size) {};
		virtual void fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) {};
		virtual void fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size) {};
		virtual void fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) {};
		virtual void fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size) {};
		virtual void fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size) {};
		virtual void fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size) {};
		virtual void fvec_copy(const RfReal32* src, RfReal32* dst, size_t size) {};
		virtual void fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size) {};

		// Double data vector operations
		virtual void dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) {};
		virtual void dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size) {};
		virtual void dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) {};
		virtual void dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size) {};
		virtual void dvec_copy(const RfReal64* src, RfReal64* dst, size_t size) {};

		// Multiplication of complex vector structured as {float, float} or {double, double}
		virtual void cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) {};
		virtual void cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) {};
		virtual void cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) {};
		virtual void cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) {};

		// Convolution operations (dot product for inner loop)
		virtual RfReal32 fconv_dot_product(const RfReal32* a, const RfReal32* b, size_t size) { return 0; }
		virtual RfReal64 dconv_dot_product(const RfReal64* a, const RfReal64* b, size_t size) { return 0; }

		// Direct time-domain convolution with kernel samples
        virtual void fconv_direct_kernel(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
                                        const RfReal32* kernel, RfInt32 kernel_len,
                                        RfInt32 write_pos, RfReal32* output, RfInt32 count) {};
        virtual void dconv_direct_kernel(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
                                        const RfReal64* kernel, RfInt32 kernel_len,
                                        RfInt32 write_pos, RfReal64* output, RfInt32 count) {};

		// Partitioned convolution - processes history in SIMD-aligned chunks
        virtual void fconv_partition(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
                                    const RfReal32* kernel, RfInt32 kernel_len,
                                    RfInt32 write_pos, RfReal32* output, RfInt32 count,
                                    RfInt32 partition_size) {};
        virtual void dconv_partition(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
                                    const RfReal64* kernel, RfInt32 kernel_len,
                                    RfInt32 write_pos, RfReal64* output, RfInt32 count,
                                    RfInt32 partition_size) {};

	// FFT-specific operations for frequency-domain convolution
	virtual void cfft_butterfly(RfCplx32* data, size_t n, size_t stride) {};
	virtual void cfft_butterfly(RfCplx64* data, size_t n, size_t stride) {};
	virtual void cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle) {};
	virtual void cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle) {};
	virtual void cfft_bit_reverse(RfCplx32* data, size_t n) {};
	virtual void cfft_bit_reverse(RfCplx64* data, size_t n) {};
	virtual void cfft_scale(RfCplx32* data, size_t n, RfReal32 scale) {};
	virtual void cfft_scale(RfCplx64* data, size_t n, RfReal64 scale) {};
	
	// Complete SIMD-optimized FFT (Cooley-Tukey radix-2)
	// These perform the entire FFT in one call with full SIMD optimization
	virtual void cfft_forward(RfCplx32* data, size_t n) {};
	virtual void cfft_forward(RfCplx64* data, size_t n) {};
	virtual void cfft_inverse(RfCplx32* data, size_t n) {};
	virtual void cfft_inverse(RfCplx64* data, size_t n) {};
	
	// Conjugate all complex numbers in array (for IFFT)
	virtual void cfvec_conj(RfCplx32* data, size_t n) {};
	virtual void cfvec_conj(RfCplx64* data, size_t n) {};

	virtual ~vec_op_base() {};
	};




	/// <summary>
	/// Non-optimized vector operations. 
	/// </summary>
	class vec_op_default: public vec_op_base {
	public:

		// Float data vector operations
		void fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size) final;
		void fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size) final;
		void fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size) final;
		void fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size) final;
		void fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size) final;
		void fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size) final;
		void fvec_copy(const RfReal32* src, RfReal32* dst, size_t size) final;
		void fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size) final;

		// Double data vector operations
		void dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) final;
		void dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) final;
		void dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size) final;
		void dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size) final;
		void dvec_copy(const RfReal64* src, RfReal64* dst, size_t size) final;

		// Multiplication of complex vector structured as {float, float} or {double, double}
		virtual void cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) final;
		virtual void cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) final;
		virtual void cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) final;
		virtual void cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) final;

		// Convolution dot product operations
		RfReal32 fconv_dot_product(const RfReal32* a, const RfReal32* b, size_t size) final;
		RfReal64 dconv_dot_product(const RfReal64* a, const RfReal64* b, size_t size) final;

		// FFT-specific operations
		void cfft_butterfly(RfCplx32* data, size_t n, size_t stride) final;
		void cfft_butterfly(RfCplx64* data, size_t n, size_t stride) final;
		void cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle) final;
		void cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle) final;
		void cfft_bit_reverse(RfCplx32* data, size_t n) final;
		void cfft_bit_reverse(RfCplx64* data, size_t n) final;
		void cfft_scale(RfCplx32* data, size_t n, RfReal32 scale) final;
		void cfft_scale(RfCplx64* data, size_t n, RfReal64 scale) final;

		void fconv_direct_kernel(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
			const RfReal32* kernel, RfInt32 kernel_len,
			RfInt32 write_pos, RfReal32* output, RfInt32 count) final;
		void dconv_direct_kernel(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
					const RfReal64* kernel, RfInt32 kernel_len,
					RfInt32 write_pos, RfReal64* output, RfInt32 count) final;
		void fconv_partition(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
				const RfReal32* kernel, RfInt32 kernel_len,
				RfInt32 write_pos, RfReal32* output, RfInt32 count,
				RfInt32 partition_size) final;
		void dconv_partition(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
				const RfReal64* kernel, RfInt32 kernel_len,
				RfInt32 write_pos, RfReal64* output, RfInt32 count,
				RfInt32 partition_size) final;
		
		~vec_op_default() override {};
	};


	/// <summary>
	/// SSE instruction set accelerated vector operations. 
	/// </summary>
	class vec_op_sse : public vec_op_base {
	public:
	#ifdef RF_SSE
		// Float data vector operations
		void fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size) final;
		void fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size) final;
		void fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size) final;
		void fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size) final;
		void fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size) final;
		void fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size) final;
		void fvec_copy(const RfReal32* src, RfReal32* dst, size_t size) final;
		void fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size) final;

		// Double data vector operations
		void dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) final;
		void dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) final;
		void dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size) final;
		void dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size) final;
		void dvec_copy(const RfReal64* src, RfReal64* dst, size_t size) final;

		// Multiplication of complex vector structured as {float, float} or {double, double}
		virtual void cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) final;
		virtual void cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) final;
		virtual void cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) final;
		virtual void cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) final;

		// Convolution dot product operations
		RfReal32 fconv_dot_product(const RfReal32* a, const RfReal32* b, size_t size) final;
		RfReal64 dconv_dot_product(const RfReal64* a, const RfReal64* b, size_t size) final;

		// FFT-specific operations
		void cfft_butterfly(RfCplx32* data, size_t n, size_t stride) final;
		void cfft_butterfly(RfCplx64* data, size_t n, size_t stride) final;
		void cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle) final;
		void cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle) final;
		void cfft_bit_reverse(RfCplx32* data, size_t n) final;
		void cfft_bit_reverse(RfCplx64* data, size_t n) final;
		void cfft_scale(RfCplx32* data, size_t n, RfReal32 scale) final;
		void cfft_scale(RfCplx64* data, size_t n, RfReal64 scale) final;
		
		// Complete SIMD-optimized FFT
		void cfft_forward(RfCplx32* data, size_t n) final;
		void cfft_forward(RfCplx64* data, size_t n) final;
		void cfft_inverse(RfCplx32* data, size_t n) final;
		void cfft_inverse(RfCplx64* data, size_t n) final;
		void cfvec_conj(RfCplx32* data, size_t n) final;
		void cfvec_conj(RfCplx64* data, size_t n) final;

		void fconv_direct_kernel(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
			const RfReal32* kernel, RfInt32 kernel_len,
			RfInt32 write_pos, RfReal32* output, RfInt32 count) final;
		void dconv_direct_kernel(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
					const RfReal64* kernel, RfInt32 kernel_len,
					RfInt32 write_pos, RfReal64* output, RfInt32 count) final;
		void fconv_partition(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
				const RfReal32* kernel, RfInt32 kernel_len,
				RfInt32 write_pos, RfReal32* output, RfInt32 count,
				RfInt32 partition_size) final;
		void dconv_partition(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
				const RfReal64* kernel, RfInt32 kernel_len,
				RfInt32 write_pos, RfReal64* output, RfInt32 count,
				RfInt32 partition_size) final;
	#endif
		~vec_op_sse() override {};
	};


	/// <summary>
	/// AVX instruction set accelerated vector operations.
	/// </summary>
	class vec_op_avx : public vec_op_base {
	public:
	#ifdef RF_DETECT_AVX2
		// Float data vector operations
		void fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size) final;
		void fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size) final;
		void fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size) final;
		void fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size) final;
		void fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size) final;
		void fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size) final;
		void fvec_copy(const RfReal32* src, RfReal32* dst, size_t size) final;
		void fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size) final;

		// Double data vector operations
		void dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) final;
		void dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) final;
		void dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size) final;
		void dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size) final;
		void dvec_copy(const RfReal64* src, RfReal64* dst, size_t size) final;

		// Multiplication of complex vector structured as {float, float} or {double, double}
		virtual void cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) final;
		virtual void cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) final;
		virtual void cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) final;
		virtual void cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) final;

		// Convolution dot product operations
		RfReal32 fconv_dot_product(const RfReal32* a, const RfReal32* b, size_t size) final;
		RfReal64 dconv_dot_product(const RfReal64* a, const RfReal64* b, size_t size) final;

		// FFT-specific operations
		void cfft_butterfly(RfCplx32* data, size_t n, size_t stride) final;
		void cfft_butterfly(RfCplx64* data, size_t n, size_t stride) final;
		void cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle) final;
		void cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle) final;
		void cfft_bit_reverse(RfCplx32* data, size_t n) final;
		void cfft_bit_reverse(RfCplx64* data, size_t n) final;
		void cfft_scale(RfCplx32* data, size_t n, RfReal32 scale) final;
		void cfft_scale(RfCplx64* data, size_t n, RfReal64 scale) final;
		
		// Complete SIMD-optimized FFT
		void cfft_forward(RfCplx32* data, size_t n) final;
		void cfft_forward(RfCplx64* data, size_t n) final;
		void cfft_inverse(RfCplx32* data, size_t n) final;
		void cfft_inverse(RfCplx64* data, size_t n) final;
		void cfvec_conj(RfCplx32* data, size_t n) final;
		void cfvec_conj(RfCplx64* data, size_t n) final;

		void fconv_direct_kernel(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
			const RfReal32* kernel, RfInt32 kernel_len,
			RfInt32 write_pos, RfReal32* output, RfInt32 count) final;
		void dconv_direct_kernel(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
					const RfReal64* kernel, RfInt32 kernel_len,
					RfInt32 write_pos, RfReal64* output, RfInt32 count) final;
		void fconv_partition(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
				const RfReal32* kernel, RfInt32 kernel_len,
				RfInt32 write_pos, RfReal32* output, RfInt32 count,
				RfInt32 partition_size) final;
		void dconv_partition(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
				const RfReal64* kernel, RfInt32 kernel_len,
				RfInt32 write_pos, RfReal64* output, RfInt32 count,
				RfInt32 partition_size) final;
	#endif
		~vec_op_avx() override {};
	};


	/// <summary>
	/// NEON instruction set accelerated vector operations.
	/// </summary>
	class vec_op_neon : public vec_op_base {
	public:
	#ifdef RF_NEON
		// Float data vector operations
		void fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size) final;
		void fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size) final;
		void fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size) final;
		void fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size) final;
		void fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size) final;
		void fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size) final;
		void fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size) final;
		void fvec_copy(const RfReal32* src, RfReal32* dst, size_t size) final;
		void fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size) final;

		// Double data vector operations
		void dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) final;
		void dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size) final;
		void dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size) final;
		void dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size) final;
		void dvec_copy(const RfReal64* src, RfReal64* dst, size_t size) final;

		// Multiplication of complex vector structured as {float, float} or {double, double}
		virtual void cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) final;
		virtual void cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) final;
		virtual void cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size) final;
		virtual void cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size) final;

		// Convolution dot product operations
		RfReal32 fconv_dot_product(const RfReal32* a, const RfReal32* b, size_t size) final;
		RfReal64 dconv_dot_product(const RfReal64* a, const RfReal64* b, size_t size) final;

		// FFT-specific operations
		void cfft_butterfly(RfCplx32* data, size_t n, size_t stride) final;
		void cfft_butterfly(RfCplx64* data, size_t n, size_t stride) final;
		void cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle) final;
		void cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle) final;
		void cfft_bit_reverse(RfCplx32* data, size_t n) final;
		void cfft_bit_reverse(RfCplx64* data, size_t n) final;
		void cfft_scale(RfCplx32* data, size_t n, RfReal32 scale) final;
		void cfft_scale(RfCplx64* data, size_t n, RfReal64 scale) final;
		
		// Complete SIMD-optimized FFT
		void cfft_forward(RfCplx32* data, size_t n) final;
		void cfft_forward(RfCplx64* data, size_t n) final;
		void cfft_inverse(RfCplx32* data, size_t n) final;
		void cfft_inverse(RfCplx64* data, size_t n) final;
		void cfvec_conj(RfCplx32* data, size_t n) final;
		void cfvec_conj(RfCplx64* data, size_t n) final;

		void fconv_direct_kernel(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
			const RfReal32* kernel, RfInt32 kernel_len,
			RfInt32 write_pos, RfReal32* output, RfInt32 count) final;
		void dconv_direct_kernel(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
					const RfReal64* kernel, RfInt32 kernel_len,
					RfInt32 write_pos, RfReal64* output, RfInt32 count) final;
		void fconv_partition(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
				const RfReal32* kernel, RfInt32 kernel_len,
				RfInt32 write_pos, RfReal32* output, RfInt32 count,
				RfInt32 partition_size) final;
		void dconv_partition(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
				const RfReal64* kernel, RfInt32 kernel_len,
				RfInt32 write_pos, RfReal64* output, RfInt32 count,
				RfInt32 partition_size) final;
	#endif
		~vec_op_neon() override {};
	};

	// -----------------------------------------------
	// Implementation control

	/// <summary>
	/// Use this as the entry of vec operators. 
	/// </summary>
	class RfRapidVec{

	private:
		/// <summary>
		/// Singleton handle.
		/// </summary>
		inline static std::shared_ptr<vec_op_base> _op = nullptr;

		/// <summary>
		/// Select op object for target platform. 
		/// </summary>
		static void select_platform();

	public:

		/// <summary>
		/// Initialize the rapid vector module.
		/// Also enables FTZ/DAZ (Flush-To-Zero / Denormals-Are-Zero) mode
		/// to prevent denormal performance penalties in audio processing.
		/// </summary>
		static void init();
		
		/// <summary>
		/// Check if FTZ/DAZ denormal handling is currently enabled.
		/// </summary>
		/// <returns>true if denormal handling is active</returns>
		static bool isDenormalHandlingEnabled();

		// Float data vector operations
		static void fvec_add(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size);
		static void fvec_mlt(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size);
		static void fvec_add(const RfReal32* in_vec1, const RfReal32 add, RfReal32* out_vec, size_t size);
		static void fvec_mlt(const RfReal32* in_vec1, const RfReal32 mul, RfReal32* out_vec, size_t size);
		static void fvec_sub(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size);
		static void fvec_sub(const RfReal32* in_vec1, const RfReal32 sub, RfReal32* out_vec, size_t size);
		static void fvec_div(const RfReal32* in_vec1, const RfReal32* in_vec2, RfReal32* out_vec, size_t size);
		static void fvec_div(const RfReal32* in_vec1, const RfReal32 div, RfReal32* out_vec, size_t size);
		static void fvec_sum(const RfReal32* in_vec, RfReal32* out, size_t size);
		static void fvec_abssum(const RfReal32* in_vec, RfReal32* out, size_t size);
		static void fvec_copy(const RfReal32* src, RfReal32* dst, size_t size);
		static void fvec_set_scalar(const RfReal32* value, RfReal32* dst, size_t size);

		// Double data vector operations
		static void dvec_add(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size);
		static void dvec_mlt(const RfReal64* in_vec1, const RfReal64* in_vec2, RfReal64* out_vec, size_t size);
		static void dvec_add(const RfReal64* in_vec1, const RfReal64 add, RfReal64* out_vec, size_t size);
		static void dvec_mlt(const RfReal64* in_vec1, const RfReal64 mul, RfReal64* out_vec, size_t size);
		static void dvec_copy(const RfReal64* src, RfReal64* dst, size_t size);

		// Multiplication of complex vector structured as {float, float} or {double, double}
		static void cfvec_mlt(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size);
		static void cdvec_mlt(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size);
		static void cfvec_mlt_add(const RfCplx32* in_vec1, const RfCplx32* in_vec2, RfCplx32* out_vec, size_t size);
		static void cdvec_mlt_add(const RfCplx64* in_vec1, const RfCplx64* in_vec2, RfCplx64* out_vec, size_t size);
		
		// FFT-specific operations (helper functions)
		static void cfft_butterfly(RfCplx32* data, size_t n, size_t stride);
		static void cfft_butterfly(RfCplx64* data, size_t n, size_t stride);
		static void cfft_twiddle_mul(RfCplx32* data, size_t n, const RfCplx32* twiddle);
		static void cfft_twiddle_mul(RfCplx64* data, size_t n, const RfCplx64* twiddle);
		static void cfft_bit_reverse(RfCplx32* data, size_t n);
		static void cfft_bit_reverse(RfCplx64* data, size_t n);
		static void cfft_scale(RfCplx32* data, size_t n, RfReal32 scale);
		static void cfft_scale(RfCplx64* data, size_t n, RfReal64 scale);

		// Complete SIMD-optimized FFT (Cooley-Tukey radix-2)
		// n must be power of 2
		static void cfft_forward(RfCplx32* data, size_t n);
		static void cfft_forward(RfCplx64* data, size_t n);
		static void cfft_inverse(RfCplx32* data, size_t n);
		static void cfft_inverse(RfCplx64* data, size_t n);
		
		// Conjugate all complex numbers in array
		static void cfvec_conj(RfCplx32* data, size_t n);
		static void cfvec_conj(RfCplx64* data, size_t n);

		// Convolution dot product operations (optimized for partitioned convolution)
		static RfReal32 fconv_dot_product(const RfReal32* a, const RfReal32* b, size_t size);
		static RfReal64 dconv_dot_product(const RfReal64* a, const RfReal64* b, size_t size);

		// Direct time-domain convolution
        static void fconv_direct_kernel(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
                                       const RfReal32* kernel, RfInt32 kernel_len,
                                       RfInt32 write_pos, RfReal32* output, RfInt32 count);
        static void dconv_direct_kernel(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
                                       const RfReal64* kernel, RfInt32 kernel_len,
                                       RfInt32 write_pos, RfReal64* output, RfInt32 count);

        // Partitioned convolution
        static void fconv_partition(const RfReal32* history, RfInt32 hist_size, RfInt32 hist_mask,
                                    const RfReal32* kernel, RfInt32 kernel_len,
                                    RfInt32 write_pos, RfReal32* output, RfInt32 count,
                                    RfInt32 partition_size);
        static void dconv_partition(const RfReal64* history, RfInt32 hist_size, RfInt32 hist_mask,
                                    const RfReal64* kernel, RfInt32 kernel_len,
                                    RfInt32 write_pos, RfReal64* output, RfInt32 count,
                                    RfInt32 partition_size);

									
		// -----------------------------------------------------------------
		// For testing use only!!!!

		/// <summary>
		/// Force using default instruction set. 
		/// </summary>
		static void force_default();
		
		/// <summary>
		/// Force using SSE instruction set. 
		/// </summary>
		static void force_sse();

		/// <summary>
		/// Force using AVX instruction set. 
		/// </summary>
		static void force_avx();

		// <summary>
		/// Force using NEON instruction set. 
		/// </summary>
		static void force_neon();

	};

}

// Compatibility with old namespace

#endif
