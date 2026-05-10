; ModuleID = 'environment.armeabi-v7a.ll'
source_filename = "environment.armeabi-v7a.ll"
target datalayout = "e-m:e-p:32:32-Fi8-i64:64-v128:64:128-a:0:32-n32-S64"
target triple = "armv7-unknown-linux-android21"

%struct.ApplicationConfig = type {
	i1, ; bool uses_mono_llvm
	i1, ; bool uses_mono_aot
	i1, ; bool aot_lazy_load
	i1, ; bool uses_assembly_preload
	i1, ; bool broken_exception_transitions
	i1, ; bool jni_add_native_method_registration_attribute_present
	i1, ; bool have_runtime_config_blob
	i1, ; bool have_assemblies_blob
	i1, ; bool marshal_methods_enabled
	i1, ; bool ignore_split_configs
	i8, ; uint8_t bound_stream_io_exception_type
	i32, ; uint32_t package_naming_policy
	i32, ; uint32_t environment_variable_count
	i32, ; uint32_t system_property_count
	i32, ; uint32_t number_of_assemblies_in_apk
	i32, ; uint32_t bundled_assembly_name_width
	i32, ; uint32_t number_of_dso_cache_entries
	i32, ; uint32_t number_of_aot_cache_entries
	i32, ; uint32_t number_of_shared_libraries
	i32, ; uint32_t android_runtime_jnienv_class_token
	i32, ; uint32_t jnienv_initialize_method_token
	i32, ; uint32_t jnienv_registerjninatives_method_token
	i32, ; uint32_t jni_remapping_replacement_type_count
	i32, ; uint32_t jni_remapping_replacement_method_index_entry_count
	i32, ; uint32_t mono_components_mask
	ptr ; char* android_package_name
}

%struct.AssemblyStoreAssemblyDescriptor = type {
	i32, ; uint32_t data_offset
	i32, ; uint32_t data_size
	i32, ; uint32_t debug_data_offset
	i32, ; uint32_t debug_data_size
	i32, ; uint32_t config_data_offset
	i32 ; uint32_t config_data_size
}

%struct.AssemblyStoreRuntimeData = type {
	ptr, ; uint8_t data_start
	i32, ; uint32_t assembly_count
	i32, ; uint32_t index_entry_count
	ptr ; AssemblyStoreAssemblyDescriptor assemblies
}

%struct.AssemblyStoreSingleAssemblyRuntimeData = type {
	ptr, ; uint8_t image_data
	ptr, ; uint8_t debug_info_data
	ptr, ; uint8_t config_data
	ptr ; AssemblyStoreAssemblyDescriptor descriptor
}

%struct.DSOApkEntry = type {
	i64, ; uint64_t name_hash
	i32, ; uint32_t offset
	i32 ; int32_t fd
}

%struct.DSOCacheEntry = type {
	i64, ; uint64_t hash
	i64, ; uint64_t real_name_hash
	i1, ; bool ignore
	ptr, ; char* name
	ptr ; void* handle
}

%struct.XamarinAndroidBundledAssembly = type {
	i32, ; int32_t file_fd
	ptr, ; char* file_name
	i32, ; uint32_t data_offset
	i32, ; uint32_t data_size
	ptr, ; uint8_t data
	i32, ; uint32_t name_length
	ptr ; char* name
}

; 0x25e6972616d58
@format_tag = dso_local local_unnamed_addr constant i64 666756936985944, align 8

@mono_aot_mode_name = dso_local local_unnamed_addr constant ptr @.str.0, align 4

; Application environment variables array, name:value
@app_environment_variables = dso_local local_unnamed_addr constant [6 x ptr] [
	ptr @.env.0, ; 0
	ptr @.env.1, ; 1
	ptr @.env.2, ; 2
	ptr @.env.3, ; 3
	ptr @.env.4, ; 4
	ptr @.env.5 ; 5
], align 4

; System properties defined by the application
@app_system_properties = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 4

@application_config = dso_local local_unnamed_addr constant %struct.ApplicationConfig {
	i1 false, ; bool uses_mono_llvm
	i1 false, ; bool uses_mono_aot
	i1 false, ; bool aot_lazy_load
	i1 false, ; bool uses_assembly_preload
	i1 false, ; bool broken_exception_transitions
	i1 false, ; bool jni_add_native_method_registration_attribute_present
	i1 true, ; bool have_runtime_config_blob
	i1 true, ; bool have_assemblies_blob
	i1 false, ; bool marshal_methods_enabled
	i1 false, ; bool ignore_split_configs
	i8 0, ; uint8_t bound_stream_io_exception_type
	i32 3, ; uint32_t package_naming_policy
	i32 6, ; uint32_t environment_variable_count
	i32 0, ; uint32_t system_property_count
	i32 130, ; uint32_t number_of_assemblies_in_apk
	i32 0, ; uint32_t bundled_assembly_name_width
	i32 32, ; uint32_t number_of_dso_cache_entries
	i32 0, ; uint32_t number_of_aot_cache_entries
	i32 8, ; uint32_t number_of_shared_libraries
	i32 u0x020002cc, ; uint32_t android_runtime_jnienv_class_token
	i32 u0x06001b16, ; uint32_t jnienv_initialize_method_token
	i32 u0x06001b15, ; uint32_t jnienv_registerjninatives_method_token
	i32 0, ; uint32_t jni_remapping_replacement_type_count
	i32 0, ; uint32_t jni_remapping_replacement_method_index_entry_count
	i32 u0x00000000, ; uint32_t mono_components_mask
	ptr @.ApplicationConfig.0_android_package_name; char* android_package_name
}, align 4

; DSO cache entries
@dso_cache = dso_local local_unnamed_addr global [32 x %struct.DSOCacheEntry] [
	%struct.DSOCacheEntry {
		i64 u0x00000000093625cd, ; from name: libSystem.Security.Cryptography.Native.Android
		i64 u0x000000007d3da8be, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.4_name, ; name: libSystem.Security.Cryptography.Native.Android.so
		ptr null; void* handle
	}, ; 0
	%struct.DSOCacheEntry {
		i64 u0x000000000daac0a4, ; from name: monodroid.so
		i64 u0x00000000cbfba5ef, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.7_name, ; name: libmonodroid.so
		ptr null; void* handle
	}, ; 1
	%struct.DSOCacheEntry {
		i64 u0x00000000129d396d, ; from name: System.Globalization.Native
		i64 u0x000000001acdcfe7, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.1_name, ; name: libSystem.Globalization.Native.so
		ptr null; void* handle
	}, ; 2
	%struct.DSOCacheEntry {
		i64 u0x00000000175bc058, ; from name: mono-component-marshal-ilgen.so
		i64 u0x000000007be514c4, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.5_name, ; name: libmono-component-marshal-ilgen.so
		ptr null; void* handle
	}, ; 3
	%struct.DSOCacheEntry {
		i64 u0x000000001acdcfe7, ; from name: libSystem.Globalization.Native.so
		i64 u0x000000001acdcfe7, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.1_name, ; name: libSystem.Globalization.Native.so
		ptr null; void* handle
	}, ; 4
	%struct.DSOCacheEntry {
		i64 u0x000000001dd6b3a3, ; from name: System.Native.so
		i64 u0x0000000079d6a0ba, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.3_name, ; name: libSystem.Native.so
		ptr null; void* handle
	}, ; 5
	%struct.DSOCacheEntry {
		i64 u0x000000002c9b28d2, ; from name: monodroid
		i64 u0x00000000cbfba5ef, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.7_name, ; name: libmonodroid.so
		ptr null; void* handle
	}, ; 6
	%struct.DSOCacheEntry {
		i64 u0x0000000033e41c10, ; from name: System.Security.Cryptography.Native.Android.so
		i64 u0x000000007d3da8be, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.4_name, ; name: libSystem.Security.Cryptography.Native.Android.so
		ptr null; void* handle
	}, ; 7
	%struct.DSOCacheEntry {
		i64 u0x000000005360f89d, ; from name: System.Security.Cryptography.Native.Android
		i64 u0x000000007d3da8be, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.4_name, ; name: libSystem.Security.Cryptography.Native.Android.so
		ptr null; void* handle
	}, ; 8
	%struct.DSOCacheEntry {
		i64 u0x000000005825b448, ; from name: libmono-component-marshal-ilgen
		i64 u0x000000007be514c4, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.5_name, ; name: libmono-component-marshal-ilgen.so
		ptr null; void* handle
	}, ; 9
	%struct.DSOCacheEntry {
		i64 u0x000000005b9ade60, ; from name: libSystem.Native
		i64 u0x0000000079d6a0ba, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.3_name, ; name: libSystem.Native.so
		ptr null; void* handle
	}, ; 10
	%struct.DSOCacheEntry {
		i64 u0x0000000063dbfd2d, ; from name: e_sqlite3
		i64 u0x00000000deed9f74, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.0_name, ; name: libe_sqlite3.so
		ptr null; void* handle
	}, ; 11
	%struct.DSOCacheEntry {
		i64 u0x0000000074cebc58, ; from name: System.IO.Compression.Native
		i64 u0x00000000af29a07d, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.2_name, ; name: libSystem.IO.Compression.Native.so
		ptr null; void* handle
	}, ; 12
	%struct.DSOCacheEntry {
		i64 u0x0000000079d6a0ba, ; from name: libSystem.Native.so
		i64 u0x0000000079d6a0ba, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.3_name, ; name: libSystem.Native.so
		ptr null; void* handle
	}, ; 13
	%struct.DSOCacheEntry {
		i64 u0x000000007b8c1361, ; from name: System.IO.Compression.Native.so
		i64 u0x00000000af29a07d, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.2_name, ; name: libSystem.IO.Compression.Native.so
		ptr null; void* handle
	}, ; 14
	%struct.DSOCacheEntry {
		i64 u0x000000007be514c4, ; from name: libmono-component-marshal-ilgen.so
		i64 u0x000000007be514c4, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.5_name, ; name: libmono-component-marshal-ilgen.so
		ptr null; void* handle
	}, ; 15
	%struct.DSOCacheEntry {
		i64 u0x000000007d3da8be, ; from name: libSystem.Security.Cryptography.Native.Android.so
		i64 u0x000000007d3da8be, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.4_name, ; name: libSystem.Security.Cryptography.Native.Android.so
		ptr null; void* handle
	}, ; 16
	%struct.DSOCacheEntry {
		i64 u0x0000000094c7a87b, ; from name: libmonosgen-2.0
		i64 u0x00000000e391d1b5, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.6_name, ; name: libmonosgen-2.0.so
		ptr null; void* handle
	}, ; 17
	%struct.DSOCacheEntry {
		i64 u0x0000000099abd194, ; from name: System.Native
		i64 u0x0000000079d6a0ba, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.3_name, ; name: libSystem.Native.so
		ptr null; void* handle
	}, ; 18
	%struct.DSOCacheEntry {
		i64 u0x000000009e770032, ; from name: monosgen-2.0.so
		i64 u0x00000000e391d1b5, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.6_name, ; name: libmonosgen-2.0.so
		ptr null; void* handle
	}, ; 19
	%struct.DSOCacheEntry {
		i64 u0x00000000a66f1e5a, ; from name: libSystem.Globalization.Native
		i64 u0x000000001acdcfe7, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.1_name, ; name: libSystem.Globalization.Native.so
		ptr null; void* handle
	}, ; 20
	%struct.DSOCacheEntry {
		i64 u0x00000000aaa0f888, ; from name: e_sqlite3.so
		i64 u0x00000000deed9f74, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.0_name, ; name: libe_sqlite3.so
		ptr null; void* handle
	}, ; 21
	%struct.DSOCacheEntry {
		i64 u0x00000000af29a07d, ; from name: libSystem.IO.Compression.Native.so
		i64 u0x00000000af29a07d, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.2_name, ; name: libSystem.IO.Compression.Native.so
		ptr null; void* handle
	}, ; 22
	%struct.DSOCacheEntry {
		i64 u0x00000000afe3142c, ; from name: libSystem.IO.Compression.Native
		i64 u0x00000000af29a07d, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.2_name, ; name: libSystem.IO.Compression.Native.so
		ptr null; void* handle
	}, ; 23
	%struct.DSOCacheEntry {
		i64 u0x00000000cbfba5ef, ; from name: libmonodroid.so
		i64 u0x00000000cbfba5ef, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.7_name, ; name: libmonodroid.so
		ptr null; void* handle
	}, ; 24
	%struct.DSOCacheEntry {
		i64 u0x00000000d8bef4d7, ; from name: libmonodroid
		i64 u0x00000000cbfba5ef, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.7_name, ; name: libmonodroid.so
		ptr null; void* handle
	}, ; 25
	%struct.DSOCacheEntry {
		i64 u0x00000000db3258f7, ; from name: libe_sqlite3
		i64 u0x00000000deed9f74, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.0_name, ; name: libe_sqlite3.so
		ptr null; void* handle
	}, ; 26
	%struct.DSOCacheEntry {
		i64 u0x00000000deed9f74, ; from name: libe_sqlite3.so
		i64 u0x00000000deed9f74, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.0_name, ; name: libe_sqlite3.so
		ptr null; void* handle
	}, ; 27
	%struct.DSOCacheEntry {
		i64 u0x00000000e1ed3ce0, ; from name: monosgen-2.0
		i64 u0x00000000e391d1b5, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.6_name, ; name: libmonosgen-2.0.so
		ptr null; void* handle
	}, ; 28
	%struct.DSOCacheEntry {
		i64 u0x00000000e391d1b5, ; from name: libmonosgen-2.0.so
		i64 u0x00000000e391d1b5, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.6_name, ; name: libmonosgen-2.0.so
		ptr null; void* handle
	}, ; 29
	%struct.DSOCacheEntry {
		i64 u0x00000000f39dc351, ; from name: mono-component-marshal-ilgen
		i64 u0x000000007be514c4, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.5_name, ; name: libmono-component-marshal-ilgen.so
		ptr null; void* handle
	}, ; 30
	%struct.DSOCacheEntry {
		i64 u0x00000000f4494c74, ; from name: System.Globalization.Native.so
		i64 u0x000000001acdcfe7, ; uint64_t real_name_hash
		i1 false, ; bool ignore
		ptr @.DSOCacheEntry.1_name, ; name: libSystem.Globalization.Native.so
		ptr null; void* handle
	} ; 31
], align 8

; AOT DSO cache entries
@aot_dso_cache = dso_local local_unnamed_addr global [0 x %struct.DSOCacheEntry] zeroinitializer, align 8

@dso_apk_entries = dso_local local_unnamed_addr global [8 x %struct.DSOApkEntry] zeroinitializer, align 8

; Bundled assembly name buffers, all empty (unused when assembly stores are enabled)
@bundled_assemblies = dso_local local_unnamed_addr global [0 x %struct.XamarinAndroidBundledAssembly] zeroinitializer, align 4

@assembly_store_bundled_assemblies = dso_local local_unnamed_addr global [130 x %struct.AssemblyStoreSingleAssemblyRuntimeData] zeroinitializer, align 4

@assembly_store = dso_local local_unnamed_addr global %struct.AssemblyStoreRuntimeData {
	ptr null, ; uint8_t* data_start
	i32 0, ; uint32_t assembly_count
	i32 0, ; uint32_t index_entry_count
	ptr null; AssemblyStoreAssemblyDescriptor* assemblies
}, align 4

; Strings
@.str.0 = private unnamed_addr constant [5 x i8] c"none\00", align 1

; Application environment variables name:value pairs
@.env.0 = private unnamed_addr constant [15 x i8] c"MONO_GC_PARAMS\00", align 1
@.env.1 = private unnamed_addr constant [21 x i8] c"major=marksweep-conc\00", align 1
@.env.2 = private unnamed_addr constant [28 x i8] c"XA_HTTP_CLIENT_HANDLER_TYPE\00", align 1
@.env.3 = private unnamed_addr constant [42 x i8] c"Xamarin.Android.Net.AndroidMessageHandler\00", align 1
@.env.4 = private unnamed_addr constant [29 x i8] c"__XA_PACKAGE_NAMING_POLICY__\00", align 1
@.env.5 = private unnamed_addr constant [15 x i8] c"LowercaseCrc64\00", align 1

;ApplicationConfig
@.ApplicationConfig.0_android_package_name = private unnamed_addr constant [31 x i8] c"com.companyname.stokbarangmaui\00", align 1

;DSOCacheEntry
@.DSOCacheEntry.0_name = private unnamed_addr constant [16 x i8] c"libe_sqlite3.so\00", align 1
@.DSOCacheEntry.1_name = private unnamed_addr constant [34 x i8] c"libSystem.Globalization.Native.so\00", align 1
@.DSOCacheEntry.2_name = private unnamed_addr constant [35 x i8] c"libSystem.IO.Compression.Native.so\00", align 1
@.DSOCacheEntry.3_name = private unnamed_addr constant [20 x i8] c"libSystem.Native.so\00", align 1
@.DSOCacheEntry.4_name = private unnamed_addr constant [50 x i8] c"libSystem.Security.Cryptography.Native.Android.so\00", align 1
@.DSOCacheEntry.5_name = private unnamed_addr constant [35 x i8] c"libmono-component-marshal-ilgen.so\00", align 1
@.DSOCacheEntry.6_name = private unnamed_addr constant [19 x i8] c"libmonosgen-2.0.so\00", align 1
@.DSOCacheEntry.7_name = private unnamed_addr constant [16 x i8] c"libmonodroid.so\00", align 1

; Metadata
!llvm.module.flags = !{!0, !1, !7}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!".NET for Android remotes/origin/release/9.0.1xx @ 1dcfb6f8779c33b6f768c996495cb90ecd729329"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"min_enum_size", i32 4}
