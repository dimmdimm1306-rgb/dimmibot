; ModuleID = 'marshal_methods.x86.ll'
source_filename = "marshal_methods.x86.ll"
target datalayout = "e-m:e-p:32:32-p270:32:32-p271:32:32-p272:64:64-f64:32:64-f80:32-n8:16:32-S128"
target triple = "i686-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [130 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [390 x i32] [
	i32 u0x00345a11, ; 0: lib_System.Net.Requests.dll.so => 105
	i32 u0x00c8cc5d, ; 1: lib_Xamarin.AndroidX.Loader.dll.so => 69
	i32 u0x0119bc86, ; 2: lib_Microsoft.Extensions.DependencyInjection.Abstractions.dll.so => 38
	i32 u0x02664405, ; 3: lib-uk-Microsoft.Maui.Controls.resources.dll.so => 29
	i32 u0x028aa24d, ; 4: System.Threading.Thread => 120
	i32 u0x03358480, ; 5: lib_Microsoft.Maui.dll.so => 45
	i32 u0x0335cdbc, ; 6: ca/Microsoft.Maui.Controls.resources => 1
	i32 u0x044bb714, ; 7: Microsoft.Maui.Graphics.dll => 47
	i32 u0x056606a6, ; 8: lib_System.Collections.NonGeneric.dll.so => 85
	i32 u0x06c2cd46, ; 9: zh-HK/Microsoft.Maui.Controls.resources => 31
	i32 u0x06ffddbc, ; 10: System.Runtime.InteropServices => 112
	i32 u0x097ed3c0, ; 11: System.ComponentModel.Annotations => 88
	i32 u0x0a0c2bd0, ; 12: lib_Xamarin.AndroidX.Activity.dll.so => 53
	i32 u0x0ade3a75, ; 13: Xamarin.AndroidX.SwipeRefreshLayout.dll => 76
	i32 u0x0aee6a3d, ; 14: lib-vi-Microsoft.Maui.Controls.resources.dll.so => 30
	i32 u0x0aeedc53, ; 15: lib_Xamarin.Google.Android.Material.dll.so => 79
	i32 u0x0b721a36, ; 16: lib-pl-Microsoft.Maui.Controls.resources.dll.so => 20
	i32 u0x0ba65f85, ; 17: vi/Microsoft.Maui.Controls.resources.dll => 30
	i32 u0x0be195c3, ; 18: zh-HK/Microsoft.Maui.Controls.resources.dll => 31
	i32 u0x0c38ff48, ; 19: System.ComponentModel => 91
	i32 u0x0c7b2e71, ; 20: Xamarin.AndroidX.Browser.dll => 56
	i32 u0x0dc2f416, ; 21: lib_Xamarin.AndroidX.CustomView.dll.so => 62
	i32 u0x0e762ada, ; 22: lib-nb-Microsoft.Maui.Controls.resources.dll.so => 18
	i32 u0x10bf9929, ; 23: cs/Microsoft.Maui.Controls.resources.dll => 2
	i32 u0x113d3381, ; 24: lib-sk-Microsoft.Maui.Controls.resources.dll.so => 25
	i32 u0x13031348, ; 25: Xamarin.AndroidX.Activity.dll => 53
	i32 u0x136bf828, ; 26: lib_System.Runtime.dll.so => 115
	i32 u0x14095832, ; 27: ja/Microsoft.Maui.Controls.resources.dll => 15
	i32 u0x14afd810, ; 28: SQLitePCLRaw.lib.e_sqlite3.android.dll => 51
	i32 u0x14eaf2a7, ; 29: lib_System.ComponentModel.Annotations.dll.so => 88
	i32 u0x153e1455, ; 30: it/Microsoft.Maui.Controls.resources.dll => 14
	i32 u0x15502fa0, ; 31: cs/Microsoft.Maui.Controls.resources => 2
	i32 u0x15c177ae, ; 32: lib_Microsoft.Extensions.Configuration.dll.so => 35
	i32 u0x15e184df, ; 33: lib_System.Runtime.Loader.dll.so => 113
	i32 u0x15ebe147, ; 34: System.IO.Pipes => 99
	i32 u0x16a510e1, ; 35: System.Threading.Thread.dll => 120
	i32 u0x16fe439a, ; 36: System.Memory.dll => 102
	i32 u0x17969339, ; 37: _Microsoft.Android.Resource.Designer => 34
	i32 u0x19f6996b, ; 38: sv/Microsoft.Maui.Controls.resources.dll => 26
	i32 u0x1a61054f, ; 39: System.Collections => 87
	i32 u0x1ae0ec2c, ; 40: Xamarin.AndroidX.Fragment.dll => 64
	i32 u0x1b317bfd, ; 41: System.Web.HttpUtility.dll => 122
	i32 u0x1b5932ea, ; 42: lib_Mono.Android.Runtime.dll.so => 128
	i32 u0x1bc6ffe7, ; 43: lib_Java.Interop.dll.so => 127
	i32 u0x1bff388e, ; 44: System.dll => 124
	i32 u0x1c78d08a, ; 45: lib_System.Private.Uri.dll.so => 109
	i32 u0x1dbae811, ; 46: System.ObjectModel => 108
	i32 u0x1dd2dc50, ; 47: id/Microsoft.Maui.Controls.resources.dll => 13
	i32 u0x1e092f31, ; 48: fi/Microsoft.Maui.Controls.resources.dll => 7
	i32 u0x1e9789de, ; 49: Microsoft.Extensions.Primitives.dll => 42
	i32 u0x1f6bf43d, ; 50: hi/Microsoft.Maui.Controls.resources => 10
	i32 u0x20216150, ; 51: Microsoft.Extensions.Logging => 39
	i32 u0x234b6fb2, ; 52: pt-BR/Microsoft.Maui.Controls.resources.dll => 21
	i32 u0x2397454a, ; 53: lib_System.Collections.Specialized.dll.so => 86
	i32 u0x2459aaf0, ; 54: lib_System.Net.Sockets.dll.so => 106
	i32 u0x2568904f, ; 55: Xamarin.AndroidX.CustomView => 62
	i32 u0x262d781c, ; 56: lib-de-Microsoft.Maui.Controls.resources.dll.so => 4
	i32 u0x27787397, ; 57: System.Text.Encodings.Web.dll => 117
	i32 u0x2814a96c, ; 58: System.Collections.Concurrent => 84
	i32 u0x28607aa1, ; 59: lib-pt-BR-Microsoft.Maui.Controls.resources.dll.so => 21
	i32 u0x2904cf94, ; 60: ca/Microsoft.Maui.Controls.resources.dll => 1
	i32 u0x29423679, ; 61: lib_Xamarin.AndroidX.CursorAdapter.dll.so => 61
	i32 u0x2a1e8ecb, ; 62: ko/Microsoft.Maui.Controls.resources.dll => 16
	i32 u0x2a4afd4a, ; 63: de/Microsoft.Maui.Controls.resources.dll => 4
	i32 u0x2b15ed29, ; 64: System.Runtime.Loader.dll => 113
	i32 u0x2ca248c0, ; 65: SQLitePCLRaw.batteries_v2 => 49
	i32 u0x2d445acd, ; 66: System.Net.Requests => 105
	i32 u0x2d745423, ; 67: System.IO.Pipes.dll => 99
	i32 u0x2e394f87, ; 68: System.IO.Compression => 97
	i32 u0x2f0980eb, ; 69: Microsoft.Extensions.Options => 41
	i32 u0x30a0e95c, ; 70: lib_System.Threading.Thread.dll.so => 120
	i32 u0x311247b5, ; 71: System.Private.Uri.dll => 109
	i32 u0x317d5b75, ; 72: System.IO.Compression.Brotli => 96
	i32 u0x3312831d, ; 73: lib_Xamarin.AndroidX.DrawerLayout.dll.so => 63
	i32 u0x33e88be1, ; 74: ar/Microsoft.Maui.Controls.resources => 0
	i32 u0x34a66c56, ; 75: lib_System.IO.Pipes.dll.so => 99
	i32 u0x351454c7, ; 76: lib_SQLitePCLRaw.lib.e_sqlite3.android.dll.so => 51
	i32 u0x35e25008, ; 77: System.ComponentModel.Primitives.dll => 89
	i32 u0x373f6a31, ; 78: tr/Microsoft.Maui.Controls.resources.dll => 28
	i32 u0x37ea9cd7, ; 79: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll.so => 67
	i32 u0x38d89c1d, ; 80: lib_Xamarin.AndroidX.Lifecycle.Common.Jvm.dll.so => 65
	i32 u0x3b2c715c, ; 81: System.Collections.dll => 87
	i32 u0x3b3271e4, ; 82: zh-Hans/Microsoft.Maui.Controls.resources => 32
	i32 u0x3b4797e5, ; 83: es/Microsoft.Maui.Controls.resources => 6
	i32 u0x3c5e5b62, ; 84: Xamarin.AndroidX.SavedState.dll => 75
	i32 u0x3d548d92, ; 85: Microsoft.Extensions.DependencyInjection.Abstractions => 38
	i32 u0x3d5a6611, ; 86: da/Microsoft.Maui.Controls.resources.dll => 3
	i32 u0x3dbaaf8f, ; 87: Xamarin.AndroidX.AppCompat => 54
	i32 u0x3e444eb4, ; 88: System.Linq.Expressions.dll => 100
	i32 u0x3ebd41f6, ; 89: lib_System.Collections.dll.so => 87
	i32 u0x3eea4db8, ; 90: lib_Microsoft.Extensions.Primitives.dll.so => 42
	i32 u0x408b17f4, ; 91: System.ComponentModel.TypeConverter => 90
	i32 u0x409e66d8, ; 92: Xamarin.Kotlin.StdLib => 80
	i32 u0x41761b2c, ; 93: System => 124
	i32 u0x42be2972, ; 94: lib_System.Text.Encodings.Web.dll.so => 117
	i32 u0x4393e151, ; 95: lib-th-Microsoft.Maui.Controls.resources.dll.so => 27
	i32 u0x444e5c8e, ; 96: lib_System.ComponentModel.TypeConverter.dll.so => 90
	i32 u0x4474042c, ; 97: lib_System.Numerics.Vectors.dll.so => 107
	i32 u0x44845810, ; 98: lib_System.Net.Http.dll.so => 103
	i32 u0x463a8801, ; 99: Xamarin.AndroidX.Navigation.Runtime.dll => 72
	i32 u0x464305ed, ; 100: fi/Microsoft.Maui.Controls.resources => 7
	i32 u0x47b79c15, ; 101: pl/Microsoft.Maui.Controls.resources.dll => 20
	i32 u0x480a69ad, ; 102: System.Diagnostics.Process => 94
	i32 u0x499b8219, ; 103: nb/Microsoft.Maui.Controls.resources.dll => 18
	i32 u0x4a0189ae, ; 104: lib-hi-Microsoft.Maui.Controls.resources.dll.so => 10
	i32 u0x4a4cd262, ; 105: Xamarin.AndroidX.Collection.Jvm.dll => 58
	i32 u0x4ae97402, ; 106: lib_Microsoft.Maui.Graphics.dll.so => 47
	i32 u0x4b275854, ; 107: Xamarin.KotlinX.Serialization.Core.Jvm => 82
	i32 u0x4d0585a0, ; 108: SQLitePCLRaw.core.dll => 50
	i32 u0x4d14ee2b, ; 109: Xamarin.AndroidX.DrawerLayout.dll => 63
	i32 u0x4eed2679, ; 110: System.Linq => 101
	i32 u0x50255dd9, ; 111: lib-hr-Microsoft.Maui.Controls.resources.dll.so => 11
	i32 u0x50acdfd7, ; 112: lib-ca-Microsoft.Maui.Controls.resources.dll.so => 1
	i32 u0x52114ed3, ; 113: Xamarin.AndroidX.SavedState => 75
	i32 u0x533678bd, ; 114: lib_System.Private.CoreLib.dll.so => 126
	i32 u0x53cefc50, ; 115: Xamarin.AndroidX.CoordinatorLayout => 59
	i32 u0x55ab7451, ; 116: Xamarin.AndroidX.Lifecycle.Common.Jvm => 65
	i32 u0x55e55df2, ; 117: Xamarin.AndroidX.Lifecycle.ViewModel.Android => 67
	i32 u0x568cd628, ; 118: System.Formats.Asn1.dll => 95
	i32 u0x57261233, ; 119: System.IO.Compression.dll => 97
	i32 u0x57924923, ; 120: Xamarin.AndroidX.AppCompat.AppCompatResources => 55
	i32 u0x57a5e912, ; 121: Microsoft.Extensions.Primitives => 42
	i32 u0x57d4e229, ; 122: StokBarangMAUI.dll => 83
	i32 u0x583e844f, ; 123: System.IO.Compression.Brotli.dll => 96
	i32 u0x58fd6613, ; 124: hi/Microsoft.Maui.Controls.resources.dll => 10
	i32 u0x5a48cf6c, ; 125: el/Microsoft.Maui.Controls.resources.dll => 5
	i32 u0x5be451c7, ; 126: lib_Xamarin.AndroidX.Browser.dll.so => 56
	i32 u0x5bf8ca0f, ; 127: System.Text.RegularExpressions.dll => 119
	i32 u0x5c7be408, ; 128: sk/Microsoft.Maui.Controls.resources.dll => 25
	i32 u0x5cabc9a4, ; 129: fr/Microsoft.Maui.Controls.resources => 8
	i32 u0x5e0b6fdc, ; 130: Xamarin.KotlinX.Serialization.Core.Jvm.dll => 82
	i32 u0x5e33306d, ; 131: sv/Microsoft.Maui.Controls.resources => 26
	i32 u0x5e7321d2, ; 132: lib_System.ComponentModel.Primitives.dll.so => 89
	i32 u0x5ed5f779, ; 133: zh-Hant/Microsoft.Maui.Controls.resources => 33
	i32 u0x60b0136a, ; 134: Xamarin.AndroidX.Loader.dll => 69
	i32 u0x60d97228, ; 135: Xamarin.AndroidX.ViewPager2 => 78
	i32 u0x6188ba7e, ; 136: Xamarin.AndroidX.CursorAdapter => 61
	i32 u0x61b9038d, ; 137: System.Net.Http.dll => 103
	i32 u0x61c036ca, ; 138: System.Text.RegularExpressions => 119
	i32 u0x62021776, ; 139: lib_System.IO.Compression.dll.so => 97
	i32 u0x620a8774, ; 140: lib_System.Xml.ReaderWriter.dll.so => 123
	i32 u0x62c6282e, ; 141: System.Runtime => 115
	i32 u0x62cec1a2, ; 142: lib_Xamarin.KotlinX.Coroutines.Core.Jvm.dll.so => 81
	i32 u0x62d6ea10, ; 143: Xamarin.Google.Android.Material.dll => 79
	i32 u0x63fca3d0, ; 144: System.Net.Primitives.dll => 104
	i32 u0x641f3e5a, ; 145: System.Security.Cryptography => 116
	i32 u0x660284a1, ; 146: SQLitePCLRaw.lib.e_sqlite3.android => 51
	i32 u0x6715dc86, ; 147: Xamarin.AndroidX.CardView.dll => 57
	i32 u0x677cd287, ; 148: ro/Microsoft.Maui.Controls.resources.dll => 23
	i32 u0x68139a0d, ; 149: System.IO.Pipelines.dll => 98
	i32 u0x68f61ae4, ; 150: lib_System.Formats.Asn1.dll.so => 95
	i32 u0x690d4b7d, ; 151: lib-zh-Hant-Microsoft.Maui.Controls.resources.dll.so => 33
	i32 u0x6947f945, ; 152: Xamarin.AndroidX.SwipeRefreshLayout => 76
	i32 u0x6988f147, ; 153: Microsoft.Extensions.Logging.dll => 39
	i32 u0x69f4f41d, ; 154: lib_Xamarin.AndroidX.AppCompat.dll.so => 54
	i32 u0x6a216153, ; 155: Mono.Android.Runtime.dll => 128
	i32 u0x6a96652d, ; 156: Xamarin.AndroidX.Fragment => 64
	i32 u0x6afaf338, ; 157: lib_System.Threading.dll.so => 121
	i32 u0x6b645ada, ; 158: lib-fr-Microsoft.Maui.Controls.resources.dll.so => 8
	i32 u0x6bcd3296, ; 159: Xamarin.AndroidX.Loader => 69
	i32 u0x6be1e423, ; 160: nb/Microsoft.Maui.Controls.resources => 18
	i32 u0x6c111525, ; 161: Xamarin.Kotlin.StdLib.dll => 80
	i32 u0x6c13413e, ; 162: Xamarin.Google.Android.Material => 79
	i32 u0x6c652ce8, ; 163: Xamarin.AndroidX.Navigation.UI.dll => 73
	i32 u0x6c96614d, ; 164: hu/Microsoft.Maui.Controls.resources => 12
	i32 u0x6cff90ba, ; 165: Microsoft.Extensions.Logging.Abstractions.dll => 40
	i32 u0x6dcaebf7, ; 166: uk/Microsoft.Maui.Controls.resources.dll => 29
	i32 u0x6ec71a65, ; 167: System.Linq.Expressions => 100
	i32 u0x7070c6c0, ; 168: lib-zh-Hans-Microsoft.Maui.Controls.resources.dll.so => 32
	i32 u0x71dc7c8b, ; 169: System.Collections.NonGeneric.dll => 85
	i32 u0x72fcebde, ; 170: lib_Xamarin.AndroidX.AppCompat.AppCompatResources.dll.so => 55
	i32 u0x731dd955, ; 171: lib_Mono.Android.dll.so => 129
	i32 u0x73674b00, ; 172: lib_SQLitePCLRaw.provider.e_sqlite3.dll.so => 52
	i32 u0x73fbecbe, ; 173: lib_System.Memory.dll.so => 102
	i32 u0x74d743bf, ; 174: ja/Microsoft.Maui.Controls.resources => 15
	i32 u0x75533a5e, ; 175: Microsoft.Extensions.Configuration.dll => 35
	i32 u0x781074ce, ; 176: hr/Microsoft.Maui.Controls.resources => 11
	i32 u0x78b622b1, ; 177: ar/Microsoft.Maui.Controls.resources.dll => 0
	i32 u0x7970be4f, ; 178: lib-he-Microsoft.Maui.Controls.resources.dll.so => 9
	i32 u0x79d00016, ; 179: it/Microsoft.Maui.Controls.resources => 14
	i32 u0x79eb68ee, ; 180: System.Private.Xml => 110
	i32 u0x7a80bd4e, ; 181: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 66
	i32 u0x7b350579, ; 182: lib__Microsoft.Android.Resource.Designer.dll.so => 34
	i32 u0x7bf8cdab, ; 183: System.Runtime.dll => 115
	i32 u0x7c9bf920, ; 184: System.Numerics.Vectors => 107
	i32 u0x7d603cde, ; 185: SQLitePCLRaw.provider.e_sqlite3.dll => 52
	i32 u0x7ec9ffe9, ; 186: System.Console => 92
	i32 u0x7fb38cd2, ; 187: System.Collections.Specialized => 86
	i32 u0x7fdcdc37, ; 188: lib-ko-Microsoft.Maui.Controls.resources.dll.so => 16
	i32 u0x8030853e, ; 189: ko/Microsoft.Maui.Controls.resources => 16
	i32 u0x8044e1bd, ; 190: lib-ms-Microsoft.Maui.Controls.resources.dll.so => 17
	i32 u0x80bd55ad, ; 191: Microsoft.Maui => 45
	i32 u0x810c11c2, ; 192: ro/Microsoft.Maui.Controls.resources => 23
	i32 u0x816751d8, ; 193: lib_System.Diagnostics.DiagnosticSource.dll.so => 93
	i32 u0x820d22b3, ; 194: Microsoft.Extensions.Options.dll => 41
	i32 u0x82a8237c, ; 195: Microsoft.Extensions.Logging.Abstractions => 40
	i32 u0x82b6c85e, ; 196: System.ObjectModel.dll => 108
	i32 u0x82bb5429, ; 197: lib_System.Linq.Expressions.dll.so => 100
	i32 u0x83323b38, ; 198: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 81
	i32 u0x8334206b, ; 199: System.Net.Http => 103
	i32 u0x8628f1a4, ; 200: lib-ru-Microsoft.Maui.Controls.resources.dll.so => 24
	i32 u0x86bba59b, ; 201: lib_Microsoft.Maui.Controls.dll.so => 43
	i32 u0x871c9c1b, ; 202: Microsoft.Extensions.Configuration.Abstractions => 36
	i32 u0x875633cc, ; 203: fr/Microsoft.Maui.Controls.resources.dll => 8
	i32 u0x87a1a22b, ; 204: lib-it-Microsoft.Maui.Controls.resources.dll.so => 14
	i32 u0x87e25095, ; 205: Xamarin.AndroidX.RecyclerView.dll => 74
	i32 u0x87e7fdbb, ; 206: lib-nl-Microsoft.Maui.Controls.resources.dll.so => 19
	i32 u0x881f94da, ; 207: lib_netstandard.dll.so => 125
	i32 u0x8873eb17, ; 208: th/Microsoft.Maui.Controls.resources => 27
	i32 u0x88d8bfaa, ; 209: System.Net.Sockets => 106
	i32 u0x88ed6f27, ; 210: lib_SQLitePCLRaw.batteries_v2.dll.so => 49
	i32 u0x896b7878, ; 211: System.Private.CoreLib.dll => 126
	i32 u0x8b804dbf, ; 212: System.Runtime.InteropServices.RuntimeInformation.dll => 111
	i32 u0x8c20c628, ; 213: lib-fi-Microsoft.Maui.Controls.resources.dll.so => 7
	i32 u0x8c20f140, ; 214: lib_System.Console.dll.so => 92
	i32 u0x8c40e0db, ; 215: System.Net.Primitives => 104
	i32 u0x8c93dffb, ; 216: lib_SQLite-net.dll.so => 48
	i32 u0x8d24e767, ; 217: System.Xml.ReaderWriter.dll => 123
	i32 u0x8d3fac99, ; 218: tr/Microsoft.Maui.Controls.resources => 28
	i32 u0x8d52b2e2, ; 219: Microsoft.Extensions.Configuration => 35
	i32 u0x8dcb0101, ; 220: lib_Xamarin.AndroidX.Navigation.Fragment.dll.so => 71
	i32 u0x8e02310f, ; 221: lib-ar-Microsoft.Maui.Controls.resources.dll.so => 0
	i32 u0x8f24faee, ; 222: System.Web.HttpUtility => 122
	i32 u0x8f8c64e2, ; 223: lib_System.Private.Xml.dll.so => 110
	i32 u0x905caa9d, ; 224: nl/Microsoft.Maui.Controls.resources => 19
	i32 u0x911615a7, ; 225: lib_Xamarin.AndroidX.Fragment.dll.so => 64
	i32 u0x912896e5, ; 226: System.Console.dll => 92
	i32 u0x928c75ca, ; 227: System.Net.Sockets.dll => 106
	i32 u0x92f11675, ; 228: SQLitePCLRaw.batteries_v2.dll => 49
	i32 u0x93554fdc, ; 229: netstandard.dll => 125
	i32 u0x93918882, ; 230: Java.Interop.dll => 127
	i32 u0x93dba8a1, ; 231: Microsoft.Maui.Controls => 43
	i32 u0x9438d78e, ; 232: lib_System.Text.Json.dll.so => 118
	i32 u0x94a1db18, ; 233: lib-id-Microsoft.Maui.Controls.resources.dll.so => 13
	i32 u0x9593ae7f, ; 234: lib_Xamarin.AndroidX.SavedState.dll.so => 75
	i32 u0x963ac2da, ; 235: sk/Microsoft.Maui.Controls.resources => 25
	i32 u0x96bea474, ; 236: lib_Microsoft.Maui.Controls.Xaml.dll.so => 44
	i32 u0x9930ee42, ; 237: System.Text.Encodings.Web => 117
	i32 u0x9b500441, ; 238: Xamarin.KotlinX.Coroutines.Core.Jvm => 81
	i32 u0x9bfe3a41, ; 239: System.Private.Xml.dll => 110
	i32 u0x9c375496, ; 240: Xamarin.AndroidX.CursorAdapter.dll => 61
	i32 u0x9c96ac4c, ; 241: lib_Xamarin.AndroidX.Navigation.UI.dll.so => 73
	i32 u0x9e78dac1, ; 242: lib_Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll.so => 68
	i32 u0x9ec4cf01, ; 243: System.Runtime.Loader => 113
	i32 u0x9f7ea921, ; 244: lib_System.Runtime.InteropServices.dll.so => 112
	i32 u0xa0fb56af, ; 245: lib_System.Text.RegularExpressions.dll.so => 119
	i32 u0xa25c90e5, ; 246: lib_Xamarin.AndroidX.Core.dll.so => 60
	i32 u0xa262a30f, ; 247: System.Runtime.Numerics.dll => 114
	i32 u0xa2ce8457, ; 248: lib-es-Microsoft.Maui.Controls.resources.dll.so => 6
	i32 u0xa2e0939b, ; 249: Xamarin.AndroidX.Activity => 53
	i32 u0xa32eb6f0, ; 250: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 55
	i32 u0xa4672f3b, ; 251: Microsoft.Maui.Controls.Xaml => 44
	i32 u0xa493aa02, ; 252: lib_System.Collections.Concurrent.dll.so => 84
	i32 u0xa4caf7a7, ; 253: Microsoft.Maui.dll => 45
	i32 u0xa4e79dfd, ; 254: Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll => 67
	i32 u0xa5a0a402, ; 255: Xamarin.AndroidX.ViewPager.dll => 77
	i32 u0xa5b67c07, ; 256: Xamarin.AndroidX.Lifecycle.Common.Jvm.dll => 65
	i32 u0xa7008e0b, ; 257: Microsoft.Maui.Graphics => 47
	i32 u0xa7042ae3, ; 258: uk/Microsoft.Maui.Controls.resources => 29
	i32 u0xa741ef0b, ; 259: es/Microsoft.Maui.Controls.resources.dll => 6
	i32 u0xa744f665, ; 260: lib_Xamarin.AndroidX.Navigation.Runtime.dll.so => 72
	i32 u0xa78103bc, ; 261: Xamarin.AndroidX.CoordinatorLayout.dll => 59
	i32 u0xa81b119f, ; 262: lib_System.Security.Cryptography.dll.so => 116
	i32 u0xa8c61dcb, ; 263: nl/Microsoft.Maui.Controls.resources.dll => 19
	i32 u0xaa107fc4, ; 264: Xamarin.AndroidX.ViewPager => 77
	i32 u0xaa4e51ff, ; 265: el/Microsoft.Maui.Controls.resources => 5
	i32 u0xaa8a4878, ; 266: Microsoft.Maui.Essentials => 46
	i32 u0xabbc23e8, ; 267: lib_Xamarin.KotlinX.Serialization.Core.Jvm.dll.so => 82
	i32 u0xabdea79a, ; 268: ru/Microsoft.Maui.Controls.resources => 24
	i32 u0xad6f1e8a, ; 269: System.Private.CoreLib => 126
	i32 u0xaddb6d38, ; 270: Xamarin.AndroidX.ViewPager2.dll => 78
	i32 u0xae037813, ; 271: System.Numerics.Vectors.dll => 107
	i32 u0xaeb2d8a5, ; 272: lib_Microsoft.Extensions.Options.dll.so => 41
	i32 u0xb0682092, ; 273: System.ComponentModel.dll => 91
	i32 u0xb18af942, ; 274: Xamarin.AndroidX.DrawerLayout => 63
	i32 u0xb223fa8c, ; 275: lib-cs-Microsoft.Maui.Controls.resources.dll.so => 2
	i32 u0xb514b305, ; 276: _Microsoft.Android.Resource.Designer.dll => 34
	i32 u0xb63fa9f0, ; 277: Xamarin.AndroidX.Navigation.Common => 70
	i32 u0xb65adef9, ; 278: Mono.Android.Runtime => 128
	i32 u0xb660be12, ; 279: System.ComponentModel.Primitives => 89
	i32 u0xb6a153b2, ; 280: lib_Xamarin.AndroidX.ViewPager2.dll.so => 78
	i32 u0xb76be845, ; 281: hu/Microsoft.Maui.Controls.resources.dll => 12
	i32 u0xb8fd311b, ; 282: System.Formats.Asn1 => 95
	i32 u0xbaa520e7, ; 283: lib_System.ObjectModel.dll.so => 108
	i32 u0xbc98c93d, ; 284: lib_Xamarin.AndroidX.Collection.Jvm.dll.so => 58
	i32 u0xbd113355, ; 285: lib_Xamarin.AndroidX.Navigation.Common.dll.so => 70
	i32 u0xbd78b0c8, ; 286: Xamarin.AndroidX.Navigation.Fragment.dll => 71
	i32 u0xbff2e236, ; 287: System.Threading => 121
	i32 u0xc235e84d, ; 288: Xamarin.AndroidX.CardView => 57
	i32 u0xc3888e16, ; 289: System.ComponentModel.Annotations.dll => 88
	i32 u0xc3e9b3a2, ; 290: SQLite-net.dll => 48
	i32 u0xc52b3930, ; 291: StokBarangMAUI => 83
	i32 u0xc591efe9, ; 292: lib_Microsoft.Extensions.Configuration.Abstractions.dll.so => 36
	i32 u0xc5b097e4, ; 293: System.Net.Requests.dll => 105
	i32 u0xc5b776df, ; 294: Xamarin.AndroidX.CustomView.dll => 62
	i32 u0xc774da4f, ; 295: Xamarin.AndroidX.Navigation.Runtime => 72
	i32 u0xc821fc10, ; 296: lib_System.ComponentModel.dll.so => 91
	i32 u0xc82afec1, ; 297: System.Text.Json => 118
	i32 u0xc849ca45, ; 298: SQLitePCLRaw.core => 50
	i32 u0xc86c06e3, ; 299: Xamarin.AndroidX.Core => 60
	i32 u0xc8a662e9, ; 300: Java.Interop => 127
	i32 u0xc92a6809, ; 301: Xamarin.AndroidX.RecyclerView => 74
	i32 u0xcc5af6ee, ; 302: Microsoft.Extensions.DependencyInjection.dll => 37
	i32 u0xcc7d82b4, ; 303: netstandard => 125
	i32 u0xce3fa116, ; 304: lib_System.Diagnostics.Process.dll.so => 94
	i32 u0xce70fda2, ; 305: hr/Microsoft.Maui.Controls.resources.dll => 11
	i32 u0xcef19b37, ; 306: System.ComponentModel.TypeConverter.dll => 90
	i32 u0xcf3163e6, ; 307: Mono.Android => 129
	i32 u0xcf663a21, ; 308: ru/Microsoft.Maui.Controls.resources.dll => 24
	i32 u0xcfa20c36, ; 309: lib_Xamarin.AndroidX.SwipeRefreshLayout.dll.so => 76
	i32 u0xcfbaacae, ; 310: System.Text.Json.dll => 118
	i32 u0xd328ac54, ; 311: vi/Microsoft.Maui.Controls.resources => 30
	i32 u0xd4045e1b, ; 312: lib_System.dll.so => 124
	i32 u0xd622b752, ; 313: lib-ro-Microsoft.Maui.Controls.resources.dll.so => 23
	i32 u0xd664cdf2, ; 314: de/Microsoft.Maui.Controls.resources => 4
	i32 u0xd715a361, ; 315: System.Linq.dll => 101
	i32 u0xd7f95f5a, ; 316: da/Microsoft.Maui.Controls.resources => 3
	i32 u0xd804d57a, ; 317: System.Runtime.InteropServices.RuntimeInformation => 111
	i32 u0xd8bba49d, ; 318: lib_Xamarin.AndroidX.RecyclerView.dll.so => 74
	i32 u0xd90e5f5a, ; 319: Xamarin.AndroidX.Lifecycle.LiveData.Core => 66
	i32 u0xd930cda0, ; 320: Xamarin.AndroidX.Navigation.Fragment => 71
	i32 u0xd96cf6f7, ; 321: pt-BR/Microsoft.Maui.Controls.resources => 21
	i32 u0xd9f65f5e, ; 322: lib-el-Microsoft.Maui.Controls.resources.dll.so => 5
	i32 u0xd9fdda56, ; 323: Microsoft.Extensions.Configuration.Abstractions.dll => 36
	i32 u0xda4773dd, ; 324: he/Microsoft.Maui.Controls.resources => 9
	i32 u0xdae8aa5e, ; 325: Mono.Android.dll => 129
	i32 u0xdb7f7e5d, ; 326: Xamarin.AndroidX.Browser => 56
	i32 u0xdbb50d93, ; 327: ms/Microsoft.Maui.Controls.resources => 17
	i32 u0xdc5370c5, ; 328: lib_System.Web.HttpUtility.dll.so => 122
	i32 u0xdc68940c, ; 329: zh-Hant/Microsoft.Maui.Controls.resources.dll => 33
	i32 u0xde068c70, ; 330: Xamarin.AndroidX.Navigation.Common.dll => 70
	i32 u0xdf6f3870, ; 331: System.Diagnostics.DiagnosticSource => 93
	i32 u0xdfca27bc, ; 332: SQLitePCLRaw.provider.e_sqlite3 => 52
	i32 u0xe13414bb, ; 333: lib-hu-Microsoft.Maui.Controls.resources.dll.so => 12
	i32 u0xe1f0a5d8, ; 334: lib_Xamarin.AndroidX.ViewPager.dll.so => 77
	i32 u0xe2098b0b, ; 335: System.Collections.NonGeneric => 85
	i32 u0xe250cda6, ; 336: lib_Microsoft.Extensions.Logging.dll.so => 39
	i32 u0xe2513246, ; 337: lib_System.Runtime.Numerics.dll.so => 114
	i32 u0xe2a3f2e8, ; 338: System.Collections.Specialized.dll => 86
	i32 u0xe34ee011, ; 339: lib_System.IO.Pipelines.dll.so => 98
	i32 u0xe37fb0d2, ; 340: lib_StokBarangMAUI.dll.so => 83
	i32 u0xe3df9d2b, ; 341: System.Security.Cryptography.dll => 116
	i32 u0xe4fab729, ; 342: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 38
	i32 u0xe56ef253, ; 343: System.Runtime.InteropServices.dll => 112
	i32 u0xe625b819, ; 344: lib_Xamarin.AndroidX.CardView.dll.so => 57
	i32 u0xe70c9739, ; 345: SQLite-net => 48
	i32 u0xe7dc15ff, ; 346: zh-Hans/Microsoft.Maui.Controls.resources.dll => 32
	i32 u0xe839deed, ; 347: System.Collections.Concurrent.dll => 84
	i32 u0xe843daa0, ; 348: Xamarin.AndroidX.Core.dll => 60
	i32 u0xe90fdb70, ; 349: Xamarin.AndroidX.Collection.Jvm => 58
	i32 u0xe99f7d24, ; 350: lib-tr-Microsoft.Maui.Controls.resources.dll.so => 28
	i32 u0xea213423, ; 351: System.Xml.ReaderWriter => 123
	i32 u0xea4fb52e, ; 352: Xamarin.AndroidX.Navigation.UI => 73
	i32 u0xeab81858, ; 353: lib_Microsoft.Maui.Essentials.dll.so => 46
	i32 u0xeaf598f6, ; 354: lib_Microsoft.Extensions.Logging.Abstractions.dll.so => 40
	i32 u0xeb5560c9, ; 355: lib_System.Runtime.InteropServices.RuntimeInformation.dll.so => 111
	i32 u0xebc66336, ; 356: Xamarin.AndroidX.AppCompat.dll => 54
	i32 u0xed1090ae, ; 357: lib_System.Net.Primitives.dll.so => 104
	i32 u0xed409aea, ; 358: th/Microsoft.Maui.Controls.resources.dll => 27
	i32 u0xed96d41f, ; 359: lib_Xamarin.AndroidX.CoordinatorLayout.dll.so => 59
	i32 u0xedadd6e2, ; 360: he/Microsoft.Maui.Controls.resources.dll => 9
	i32 u0xee9f991d, ; 361: System.Diagnostics.Process.dll => 94
	i32 u0xefd01a89, ; 362: System.IO.Pipelines => 98
	i32 u0xeff49a63, ; 363: System.Memory => 102
	i32 u0xf121f953, ; 364: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.dll.so => 66
	i32 u0xf1304331, ; 365: Microsoft.Maui.Controls.Xaml.dll => 44
	i32 u0xf1676aaa, ; 366: lib-da-Microsoft.Maui.Controls.resources.dll.so => 3
	i32 u0xf29c5384, ; 367: id/Microsoft.Maui.Controls.resources => 13
	i32 u0xf2ce3c98, ; 368: System.Threading.dll => 121
	i32 u0xf2dd3fc4, ; 369: lib-ja-Microsoft.Maui.Controls.resources.dll.so => 15
	i32 u0xf323e0a6, ; 370: lib_Xamarin.Kotlin.StdLib.dll.so => 80
	i32 u0xf40add04, ; 371: Microsoft.Maui.Essentials.dll => 46
	i32 u0xf462c30d, ; 372: System.Private.Uri => 109
	i32 u0xf48143e5, ; 373: pt/Microsoft.Maui.Controls.resources.dll => 22
	i32 u0xf5185c24, ; 374: lib-pt-Microsoft.Maui.Controls.resources.dll.so => 22
	i32 u0xf5861a4f, ; 375: pl/Microsoft.Maui.Controls.resources => 20
	i32 u0xf5e94e90, ; 376: ms/Microsoft.Maui.Controls.resources.dll => 17
	i32 u0xf5f4f1f0, ; 377: Microsoft.Extensions.DependencyInjection => 37
	i32 u0xf5fdf056, ; 378: lib_Microsoft.Extensions.DependencyInjection.dll.so => 37
	i32 u0xf86129d4, ; 379: lib-sv-Microsoft.Maui.Controls.resources.dll.so => 26
	i32 u0xf94a8f86, ; 380: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 68
	i32 u0xf9be026d, ; 381: lib_SQLitePCLRaw.core.dll.so => 50
	i32 u0xfa50891f, ; 382: lib_System.Linq.dll.so => 101
	i32 u0xfb0af295, ; 383: lib-zh-HK-Microsoft.Maui.Controls.resources.dll.so => 31
	i32 u0xfb1dad5d, ; 384: System.Diagnostics.DiagnosticSource.dll => 93
	i32 u0xfbc4b67c, ; 385: lib_System.IO.Compression.Brotli.dll.so => 96
	i32 u0xfc5f7d36, ; 386: pt/Microsoft.Maui.Controls.resources => 22
	i32 u0xfea12dee, ; 387: Microsoft.Maui.Controls.dll => 43
	i32 u0xfecef6ea, ; 388: System.Runtime.Numerics => 114
	i32 u0xffd4917f ; 389: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 68
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [390 x i32] [
	i32 105, i32 69, i32 38, i32 29, i32 120, i32 45, i32 1, i32 47,
	i32 85, i32 31, i32 112, i32 88, i32 53, i32 76, i32 30, i32 79,
	i32 20, i32 30, i32 31, i32 91, i32 56, i32 62, i32 18, i32 2,
	i32 25, i32 53, i32 115, i32 15, i32 51, i32 88, i32 14, i32 2,
	i32 35, i32 113, i32 99, i32 120, i32 102, i32 34, i32 26, i32 87,
	i32 64, i32 122, i32 128, i32 127, i32 124, i32 109, i32 108, i32 13,
	i32 7, i32 42, i32 10, i32 39, i32 21, i32 86, i32 106, i32 62,
	i32 4, i32 117, i32 84, i32 21, i32 1, i32 61, i32 16, i32 4,
	i32 113, i32 49, i32 105, i32 99, i32 97, i32 41, i32 120, i32 109,
	i32 96, i32 63, i32 0, i32 99, i32 51, i32 89, i32 28, i32 67,
	i32 65, i32 87, i32 32, i32 6, i32 75, i32 38, i32 3, i32 54,
	i32 100, i32 87, i32 42, i32 90, i32 80, i32 124, i32 117, i32 27,
	i32 90, i32 107, i32 103, i32 72, i32 7, i32 20, i32 94, i32 18,
	i32 10, i32 58, i32 47, i32 82, i32 50, i32 63, i32 101, i32 11,
	i32 1, i32 75, i32 126, i32 59, i32 65, i32 67, i32 95, i32 97,
	i32 55, i32 42, i32 83, i32 96, i32 10, i32 5, i32 56, i32 119,
	i32 25, i32 8, i32 82, i32 26, i32 89, i32 33, i32 69, i32 78,
	i32 61, i32 103, i32 119, i32 97, i32 123, i32 115, i32 81, i32 79,
	i32 104, i32 116, i32 51, i32 57, i32 23, i32 98, i32 95, i32 33,
	i32 76, i32 39, i32 54, i32 128, i32 64, i32 121, i32 8, i32 69,
	i32 18, i32 80, i32 79, i32 73, i32 12, i32 40, i32 29, i32 100,
	i32 32, i32 85, i32 55, i32 129, i32 52, i32 102, i32 15, i32 35,
	i32 11, i32 0, i32 9, i32 14, i32 110, i32 66, i32 34, i32 115,
	i32 107, i32 52, i32 92, i32 86, i32 16, i32 16, i32 17, i32 45,
	i32 23, i32 93, i32 41, i32 40, i32 108, i32 100, i32 81, i32 103,
	i32 24, i32 43, i32 36, i32 8, i32 14, i32 74, i32 19, i32 125,
	i32 27, i32 106, i32 49, i32 126, i32 111, i32 7, i32 92, i32 104,
	i32 48, i32 123, i32 28, i32 35, i32 71, i32 0, i32 122, i32 110,
	i32 19, i32 64, i32 92, i32 106, i32 49, i32 125, i32 127, i32 43,
	i32 118, i32 13, i32 75, i32 25, i32 44, i32 117, i32 81, i32 110,
	i32 61, i32 73, i32 68, i32 113, i32 112, i32 119, i32 60, i32 114,
	i32 6, i32 53, i32 55, i32 44, i32 84, i32 45, i32 67, i32 77,
	i32 65, i32 47, i32 29, i32 6, i32 72, i32 59, i32 116, i32 19,
	i32 77, i32 5, i32 46, i32 82, i32 24, i32 126, i32 78, i32 107,
	i32 41, i32 91, i32 63, i32 2, i32 34, i32 70, i32 128, i32 89,
	i32 78, i32 12, i32 95, i32 108, i32 58, i32 70, i32 71, i32 121,
	i32 57, i32 88, i32 48, i32 83, i32 36, i32 105, i32 62, i32 72,
	i32 91, i32 118, i32 50, i32 60, i32 127, i32 74, i32 37, i32 125,
	i32 94, i32 11, i32 90, i32 129, i32 24, i32 76, i32 118, i32 30,
	i32 124, i32 23, i32 4, i32 101, i32 3, i32 111, i32 74, i32 66,
	i32 71, i32 21, i32 5, i32 36, i32 9, i32 129, i32 56, i32 17,
	i32 122, i32 33, i32 70, i32 93, i32 52, i32 12, i32 77, i32 85,
	i32 39, i32 114, i32 86, i32 98, i32 83, i32 116, i32 38, i32 112,
	i32 57, i32 48, i32 32, i32 84, i32 60, i32 58, i32 28, i32 123,
	i32 73, i32 46, i32 40, i32 111, i32 54, i32 104, i32 27, i32 59,
	i32 9, i32 94, i32 98, i32 102, i32 66, i32 44, i32 3, i32 13,
	i32 121, i32 15, i32 80, i32 46, i32 109, i32 22, i32 22, i32 20,
	i32 17, i32 37, i32 37, i32 26, i32 68, i32 50, i32 101, i32 31,
	i32 93, i32 96, i32 22, i32 43, i32 114, i32 68
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 4

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 4

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 u0x0000000000000000, ; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 4

; Functions

; Function attributes: memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.str.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 4, !tbaa !3
	ret void
}

; Strings
@.str.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 1

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "stackrealign" "target-cpu"="i686" "target-features"="+cx8,+mmx,+sse,+sse2,+sse3,+ssse3,+x87" "tune-cpu"="generic" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "stackrealign" "target-cpu"="i686" "target-features"="+cx8,+mmx,+sse,+sse2,+sse3,+ssse3,+x87" "tune-cpu"="generic" }

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
!7 = !{i32 1, !"NumRegisterParameters", i32 0}
