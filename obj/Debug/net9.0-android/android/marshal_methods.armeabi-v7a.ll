; ModuleID = 'marshal_methods.armeabi-v7a.ll'
source_filename = "marshal_methods.armeabi-v7a.ll"
target datalayout = "e-m:e-p:32:32-Fi8-i64:64-v128:64:128-a:0:32-n32-S64"
target triple = "armv7-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [318 x ptr] zeroinitializer, align 4

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [954 x i32] [
	i32 u0x0027eb9e, ; 0: System.Net.NetworkInformation.dll => 69
	i32 u0x00345a11, ; 1: lib_System.Net.Requests.dll.so => 73
	i32 u0x009b21bb, ; 2: System.Net.NameResolution.dll => 68
	i32 u0x00c8cc5d, ; 3: lib_Xamarin.AndroidX.Loader.dll.so => 244
	i32 u0x00e0bbf7, ; 4: lib_System.Xml.XmlSerializer.dll.so => 163
	i32 u0x00efe298, ; 5: System.Runtime.Intrinsics.dll => 109
	i32 u0x0119bc86, ; 6: lib_Microsoft.Extensions.DependencyInjection.Abstractions.dll.so => 178
	i32 u0x01f2c4e1, ; 7: Xamarin.AndroidX.Lifecycle.Runtime => 236
	i32 u0x0211b5dc, ; 8: Xamarin.Google.Guava.ListenableFuture.dll => 272
	i32 u0x02139ac3, ; 9: System.IO.FileSystem.DriveInfo => 48
	i32 u0x025a8054, ; 10: System.Net.WebSockets.dll => 81
	i32 u0x02664405, ; 11: lib-uk-Microsoft.Maui.Controls.resources.dll.so => 312
	i32 u0x028aa24d, ; 12: System.Threading.Thread => 146
	i32 u0x02bda0f5, ; 13: Xamarin.KotlinX.AtomicFU.Jvm => 277
	i32 u0x03358480, ; 14: lib_Microsoft.Maui.dll.so => 186
	i32 u0x0335cdbc, ; 15: ca/Microsoft.Maui.Controls.resources => 284
	i32 u0x03f75868, ; 16: System.Diagnostics.StackTrace => 30
	i32 u0x0410f24b, ; 17: System.Security.Cryptography.Primitives => 125
	i32 u0x044bb714, ; 18: Microsoft.Maui.Graphics.dll => 188
	i32 u0x04e7b0a1, ; 19: System.Runtime.CompilerServices.VisualC.dll => 103
	i32 u0x056606a6, ; 20: lib_System.Collections.NonGeneric.dll.so => 10
	i32 u0x060d4943, ; 21: Xamarin.AndroidX.SlidingPaneLayout => 255
	i32 u0x065dd880, ; 22: lib_System.Linq.Queryable.dll.so => 61
	i32 u0x06c2cd46, ; 23: zh-HK/Microsoft.Maui.Controls.resources => 314
	i32 u0x06e4e181, ; 24: lib_Xamarin.Google.Guava.ListenableFuture.dll.so => 272
	i32 u0x06ee56d3, ; 25: lib_System.Net.Mail.dll.so => 67
	i32 u0x06ffddbc, ; 26: System.Runtime.InteropServices => 108
	i32 u0x072f9521, ; 27: Xamarin.AndroidX.SlidingPaneLayout.dll => 255
	i32 u0x074aea82, ; 28: System.Threading.Channels.dll => 140
	i32 u0x0772c6a7, ; 29: lib_System.Diagnostics.TextWriterTraceListener.dll.so => 31
	i32 u0x0881c32f, ; 30: System.Net.WebHeaderCollection => 78
	i32 u0x08f064cf, ; 31: System.Security.Cryptography.Primitives.dll => 125
	i32 u0x097ed3c0, ; 32: System.ComponentModel.Annotations => 13
	i32 u0x098905a2, ; 33: lib_Xamarin.AndroidX.Concurrent.Futures.dll.so => 212
	i32 u0x09d975c3, ; 34: Xamarin.AndroidX.Collection.dll => 209
	i32 u0x09e60a6e, ; 35: Xamarin.KotlinX.AtomicFU.dll => 276
	i32 u0x0a0c2bd0, ; 36: lib_Xamarin.AndroidX.Activity.dll.so => 198
	i32 u0x0a81994f, ; 37: System.ServiceProcess => 133
	i32 u0x0ade3a75, ; 38: Xamarin.AndroidX.SwipeRefreshLayout.dll => 257
	i32 u0x0ae43932, ; 39: lib_Xamarin.AndroidX.Tracing.Tracing.dll.so => 258
	i32 u0x0aee6a3d, ; 40: lib-vi-Microsoft.Maui.Controls.resources.dll.so => 313
	i32 u0x0aeedc53, ; 41: lib_Xamarin.Google.Android.Material.dll.so => 267
	i32 u0x0afca281, ; 42: System.ValueTuple.dll => 152
	i32 u0x0b0de1c3, ; 43: lib_System.Xml.XPath.XDocument.dll.so => 160
	i32 u0x0b63b1e1, ; 44: lib_System.Net.Http.Json.dll.so => 64
	i32 u0x0b721a36, ; 45: lib-pl-Microsoft.Maui.Controls.resources.dll.so => 303
	i32 u0x0ba65f85, ; 46: vi/Microsoft.Maui.Controls.resources.dll => 313
	i32 u0x0ba8e231, ; 47: lib_System.Net.ServicePoint.dll.so => 75
	i32 u0x0be195c3, ; 48: zh-HK/Microsoft.Maui.Controls.resources.dll => 314
	i32 u0x0c38ff48, ; 49: System.ComponentModel => 18
	i32 u0x0c7b2e71, ; 50: Xamarin.AndroidX.Browser.dll => 207
	i32 u0x0cfa66a6, ; 51: lib_System.IO.Compression.FileSystem.dll.so => 44
	i32 u0x0d1f8edb, ; 52: System.Diagnostics.Debug => 26
	i32 u0x0d73bff4, ; 53: lib_Microsoft.Extensions.Logging.Debug.dll.so => 181
	i32 u0x0dc10265, ; 54: Microsoft.CSharp.dll => 1
	i32 u0x0dc2edec, ; 55: lib_Xamarin.AndroidX.Core.ViewTree.dll.so => 218
	i32 u0x0dc2f416, ; 56: lib_Xamarin.AndroidX.CustomView.dll.so => 220
	i32 u0x0dcb05c4, ; 57: System.Linq.Parallel => 60
	i32 u0x0dd133ce, ; 58: System.Globalization => 42
	i32 u0x0e762ada, ; 59: lib-nb-Microsoft.Maui.Controls.resources.dll.so => 301
	i32 u0x0eb2f8c5, ; 60: System.Reflection.Emit.Lightweight => 92
	i32 u0x0ec71be0, ; 61: lib_System.Security.SecureString.dll.so => 130
	i32 u0x0ecfdca9, ; 62: lib_Xamarin.Android.Glide.dll.so => 194
	i32 u0x0f99119d, ; 63: Xamarin.AndroidX.ConstraintLayout.dll => 213
	i32 u0x107abf20, ; 64: System.Threading.Timer.dll => 148
	i32 u0x109c6ab8, ; 65: Xamarin.AndroidX.Lifecycle.LiveData.dll => 232
	i32 u0x10b7d2b7, ; 66: Xamarin.AndroidX.Interpolator => 229
	i32 u0x10bf9929, ; 67: cs/Microsoft.Maui.Controls.resources.dll => 285
	i32 u0x10c1d9f6, ; 68: lib_System.Data.DataSetExtensions.dll.so => 23
	i32 u0x113d3381, ; 69: lib-sk-Microsoft.Maui.Controls.resources.dll.so => 308
	i32 u0x1159791e, ; 70: System.IO.Pipes.AccessControl.dll => 55
	i32 u0x11d123fd, ; 71: System.Net.Ping.dll => 70
	i32 u0x13031348, ; 72: Xamarin.AndroidX.Activity.dll => 198
	i32 u0x132b30dd, ; 73: System.Numerics => 84
	i32 u0x1331a702, ; 74: lib_Xamarin.Google.Crypto.Tink.Android.dll.so => 269
	i32 u0x136bf828, ; 75: lib_System.Runtime.dll.so => 117
	i32 u0x14095832, ; 76: ja/Microsoft.Maui.Controls.resources.dll => 298
	i32 u0x146817a2, ; 77: Xamarin.AndroidX.Lifecycle.Common => 230
	i32 u0x14afd810, ; 78: SQLitePCLRaw.lib.e_sqlite3.android.dll => 192
	i32 u0x14eaf2a7, ; 79: lib_System.ComponentModel.Annotations.dll.so => 13
	i32 u0x153e1455, ; 80: it/Microsoft.Maui.Controls.resources.dll => 297
	i32 u0x15502fa0, ; 81: cs/Microsoft.Maui.Controls.resources => 285
	i32 u0x15766b7b, ; 82: System.ServiceModel.Web => 132
	i32 u0x15c177ae, ; 83: lib_Microsoft.Extensions.Configuration.dll.so => 175
	i32 u0x15e184df, ; 84: lib_System.Runtime.Loader.dll.so => 110
	i32 u0x15ebe147, ; 85: System.IO.Pipes => 56
	i32 u0x1658bf94, ; 86: System.Transactions.Local => 150
	i32 u0x16646418, ; 87: System.Net.ServicePoint.dll => 75
	i32 u0x16a510e1, ; 88: System.Threading.Thread.dll => 146
	i32 u0x16fe439a, ; 89: System.Memory.dll => 63
	i32 u0x1766c1f7, ; 90: System.Threading.ThreadPool.dll => 147
	i32 u0x1778984a, ; 91: lib_Xamarin.AndroidX.ResourceInspection.Annotation.dll.so => 251
	i32 u0x17969339, ; 92: _Microsoft.Android.Resource.Designer => 317
	i32 u0x180c08d0, ; 93: WindowsBase => 166
	i32 u0x195d1904, ; 94: Xamarin.AndroidX.Lifecycle.Runtime.Android => 237
	i32 u0x198cd3eb, ; 95: lib_System.Security.Cryptography.Encoding.dll.so => 123
	i32 u0x19f6996b, ; 96: sv/Microsoft.Maui.Controls.resources.dll => 309
	i32 u0x1a4e3ec4, ; 97: Xamarin.AndroidX.ConstraintLayout.Core => 214
	i32 u0x1a61054f, ; 98: System.Collections => 12
	i32 u0x1ae0ec2c, ; 99: Xamarin.AndroidX.Fragment.dll => 227
	i32 u0x1ae969b2, ; 100: System.Security.Cryptography.X509Certificates => 126
	i32 u0x1b317bfd, ; 101: System.Web.HttpUtility.dll => 153
	i32 u0x1b46a9fd, ; 102: lib_Xamarin.AndroidX.Lifecycle.Runtime.Ktx.dll.so => 238
	i32 u0x1b5932ea, ; 103: lib_Mono.Android.Runtime.dll.so => 171
	i32 u0x1b611806, ; 104: System.Runtime.Serialization.Primitives.dll => 114
	i32 u0x1bc4415d, ; 105: mscorlib => 167
	i32 u0x1bc6ffe7, ; 106: lib_Java.Interop.dll.so => 169
	i32 u0x1bff388e, ; 107: System.dll => 165
	i32 u0x1c690cb9, ; 108: Xamarin.AndroidX.Interpolator.dll => 229
	i32 u0x1c78d08a, ; 109: lib_System.Private.Uri.dll.so => 87
	i32 u0x1d48410e, ; 110: lib_Xamarin.AndroidX.SlidingPaneLayout.dll.so => 255
	i32 u0x1d4d8185, ; 111: lib_System.Runtime.Serialization.dll.so => 116
	i32 u0x1dbae811, ; 112: System.ObjectModel => 85
	i32 u0x1dd2dc50, ; 113: id/Microsoft.Maui.Controls.resources.dll => 296
	i32 u0x1e092f31, ; 114: fi/Microsoft.Maui.Controls.resources.dll => 290
	i32 u0x1e9789de, ; 115: Microsoft.Extensions.Primitives.dll => 183
	i32 u0x1f1dceb7, ; 116: lib_System.Security.Cryptography.Primitives.dll.so => 125
	i32 u0x1f443e2d, ; 117: lib_System.AppContext.dll.so => 6
	i32 u0x1f6088c2, ; 118: System.Transactions.dll => 151
	i32 u0x1f6bf43d, ; 119: hi/Microsoft.Maui.Controls.resources => 293
	i32 u0x1f9b4faa, ; 120: System.Linq.Queryable => 61
	i32 u0x20216150, ; 121: Microsoft.Extensions.Logging => 179
	i32 u0x20303736, ; 122: System.IO.FileSystem.dll => 51
	i32 u0x2080b118, ; 123: System.Runtime.Extensions => 104
	i32 u0x20924146, ; 124: System.Runtime.Serialization.Xml => 115
	i32 u0x20bbb280, ; 125: System.Globalization.Calendars => 40
	i32 u0x2116ab2f, ; 126: Xamarin.JSpecify.dll => 274
	i32 u0x213954e7, ; 127: Jsr305Binding => 268
	i32 u0x218bdf07, ; 128: Xamarin.AndroidX.Core.ViewTree.dll => 218
	i32 u0x21f36ef8, ; 129: Xamarin.AndroidX.Window.Extensions.Core.Core => 266
	i32 u0x22697083, ; 130: System.Security.Cryptography.Cng => 121
	i32 u0x234b6fb2, ; 131: pt-BR/Microsoft.Maui.Controls.resources.dll => 304
	i32 u0x236793de, ; 132: lib_GoogleGson.dll.so => 174
	i32 u0x2386616a, ; 133: lib_System.ServiceModel.Web.dll.so => 132
	i32 u0x2397454a, ; 134: lib_System.Collections.Specialized.dll.so => 11
	i32 u0x23d83352, ; 135: System.IO.IsolatedStorage.dll => 52
	i32 u0x23eaab34, ; 136: lib_System.Core.dll.so => 21
	i32 u0x24154ecb, ; 137: System.IO.Compression.FileSystem => 44
	i32 u0x2459aaf0, ; 138: lib_System.Net.Sockets.dll.so => 76
	i32 u0x2493d7b9, ; 139: System.Security.Cryptography.Algorithms => 120
	i32 u0x2512d1c5, ; 140: Xamarin.AndroidX.Lifecycle.Runtime.Android.dll => 237
	i32 u0x2568904f, ; 141: Xamarin.AndroidX.CustomView => 220
	i32 u0x26233b86, ; 142: Xamarin.AndroidX.Emoji2.ViewsHelper.dll => 225
	i32 u0x26249f17, ; 143: lib_Xamarin.AndroidX.CustomView.PoolingContainer.dll.so => 221
	i32 u0x262968a7, ; 144: lib_System.Reflection.Extensions.dll.so => 94
	i32 u0x262d781c, ; 145: lib-de-Microsoft.Maui.Controls.resources.dll.so => 287
	i32 u0x2660a755, ; 146: System.Net => 82
	i32 u0x27787397, ; 147: System.Text.Encodings.Web.dll => 137
	i32 u0x278c7790, ; 148: Xamarin.AndroidX.VersionedParcelable => 262
	i32 u0x27b53050, ; 149: lib_System.Data.Common.dll.so => 22
	i32 u0x27b6d01f, ; 150: Xamarin.AndroidX.Arch.Core.Common.dll => 205
	i32 u0x2814a96c, ; 151: System.Collections.Concurrent => 8
	i32 u0x282acf5e, ; 152: lib_System.IO.FileSystem.dll.so => 51
	i32 u0x28607aa1, ; 153: lib-pt-BR-Microsoft.Maui.Controls.resources.dll.so => 304
	i32 u0x287c1a88, ; 154: Xamarin.KotlinX.AtomicFU => 276
	i32 u0x28bdabca, ; 155: System.Net.Security => 74
	i32 u0x2904cf94, ; 156: ca/Microsoft.Maui.Controls.resources.dll => 284
	i32 u0x29293ff5, ; 157: System.Xml.Linq.dll => 156
	i32 u0x29352520, ; 158: Xamarin.KotlinX.Coroutines.Android.dll => 278
	i32 u0x29423679, ; 159: lib_Xamarin.AndroidX.CursorAdapter.dll.so => 219
	i32 u0x295a9e3d, ; 160: System.Windows => 155
	i32 u0x296c7566, ; 161: lib_System.Xml.dll.so => 164
	i32 u0x29af2b3b, ; 162: System.Reflection.Emit => 93
	i32 u0x29bd7e5b, ; 163: Xamarin.Jetbrains.Annotations => 273
	i32 u0x29be9df3, ; 164: System.IO.Compression.ZipFile => 45
	i32 u0x2a1e8ecb, ; 165: ko/Microsoft.Maui.Controls.resources.dll => 299
	i32 u0x2a4afd4a, ; 166: de/Microsoft.Maui.Controls.resources.dll => 287
	i32 u0x2aaa494f, ; 167: Xamarin.Google.ErrorProne.TypeAnnotations => 271
	i32 u0x2b15ed29, ; 168: System.Runtime.Loader.dll => 110
	i32 u0x2ba1ca8c, ; 169: lib_System.Security.dll.so => 131
	i32 u0x2bd14e96, ; 170: System.Security.SecureString.dll => 130
	i32 u0x2ca248c0, ; 171: SQLitePCLRaw.batteries_v2 => 190
	i32 u0x2cd6293c, ; 172: System.Diagnostics.Contracts.dll => 25
	i32 u0x2d052d0c, ; 173: Xamarin.Android.Glide.Annotations.dll => 195
	i32 u0x2d322560, ; 174: lib_System.Xml.XmlDocument.dll.so => 162
	i32 u0x2d445acd, ; 175: System.Net.Requests => 73
	i32 u0x2d745423, ; 176: System.IO.Pipes.dll => 56
	i32 u0x2e394f87, ; 177: System.IO.Compression => 46
	i32 u0x2eec5558, ; 178: lib_System.Reflection.dll.so => 98
	i32 u0x2f0980eb, ; 179: Microsoft.Extensions.Options => 182
	i32 u0x2f0fe5eb, ; 180: lib_System.Reflection.DispatchProxy.dll.so => 90
	i32 u0x2f1c1e69, ; 181: Xamarin.AndroidX.CustomView.PoolingContainer.dll => 221
	i32 u0x2ff6fb9f, ; 182: System.Data.Common => 22
	i32 u0x302809e9, ; 183: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx.dll => 234
	i32 u0x30a0e95c, ; 184: lib_System.Threading.Thread.dll.so => 146
	i32 u0x311247b5, ; 185: System.Private.Uri.dll => 87
	i32 u0x317d5b75, ; 186: System.IO.Compression.Brotli => 43
	i32 u0x31a103c6, ; 187: System.Xml.XPath.dll => 161
	i32 u0x31b69d60, ; 188: System.Net.Quic => 72
	i32 u0x3312831d, ; 189: lib_Xamarin.AndroidX.DrawerLayout.dll.so => 222
	i32 u0x33e88be1, ; 190: ar/Microsoft.Maui.Controls.resources => 283
	i32 u0x340ac0b8, ; 191: Microsoft.VisualBasic => 3
	i32 u0x34505120, ; 192: System.Globalization.dll => 42
	i32 u0x3463c971, ; 193: System.Net.Http.Json => 64
	i32 u0x34a66c56, ; 194: lib_System.IO.Pipes.dll.so => 56
	i32 u0x351454c7, ; 195: lib_SQLitePCLRaw.lib.e_sqlite3.android.dll.so => 192
	i32 u0x352e5794, ; 196: lib_Xamarin.Google.ErrorProne.Annotations.dll.so => 270
	i32 u0x35e25008, ; 197: System.ComponentModel.Primitives.dll => 16
	i32 u0x3612ff2c, ; 198: lib_System.IO.dll.so => 58
	i32 u0x364e69a3, ; 199: System.IO.MemoryMappedFiles.dll => 53
	i32 u0x36e9595b, ; 200: lib_System.Transactions.dll.so => 151
	i32 u0x370eff4f, ; 201: lib_System.Globalization.Extensions.dll.so => 41
	i32 u0x373f6a31, ; 202: tr/Microsoft.Maui.Controls.resources.dll => 311
	i32 u0x3751ef41, ; 203: Xamarin.Google.Guava.ListenableFuture => 272
	i32 u0x3787b992, ; 204: lib_System.ComponentModel.DataAnnotations.dll.so => 14
	i32 u0x37ea9cd7, ; 205: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll.so => 241
	i32 u0x382704bd, ; 206: lib_Xamarin.AndroidX.Emoji2.ViewsHelper.dll.so => 225
	i32 u0x38c136f7, ; 207: System.Runtime.InteropServices.JavaScript.dll => 106
	i32 u0x38d89c1d, ; 208: lib_Xamarin.AndroidX.Lifecycle.Common.Jvm.dll.so => 231
	i32 u0x39481653, ; 209: lib_mscorlib.dll.so => 167
	i32 u0x399f1f06, ; 210: Xamarin.Google.Crypto.Tink.Android => 269
	i32 u0x39adca5e, ; 211: Xamarin.AndroidX.Lifecycle.Common.dll => 230
	i32 u0x3a20ecf3, ; 212: System.Diagnostics.Tracing => 34
	i32 u0x3a2aaa1d, ; 213: System.Xml.XDocument => 159
	i32 u0x3a8b0a79, ; 214: lib_Xamarin.KotlinX.Coroutines.Android.dll.so => 278
	i32 u0x3acd0267, ; 215: System.Private.DataContractSerialization.dll => 86
	i32 u0x3ad7b407, ; 216: System.Diagnostics.Tools => 32
	i32 u0x3b008d80, ; 217: lib_Xamarin.AndroidX.DynamicAnimation.dll.so => 223
	i32 u0x3b2c715c, ; 218: System.Collections.dll => 12
	i32 u0x3b3271e4, ; 219: zh-Hans/Microsoft.Maui.Controls.resources => 315
	i32 u0x3b458447, ; 220: lib_System.Threading.Tasks.Dataflow.dll.so => 142
	i32 u0x3b45fb35, ; 221: System.IO.FileSystem => 51
	i32 u0x3b4797e5, ; 222: es/Microsoft.Maui.Controls.resources => 289
	i32 u0x3bb6bd33, ; 223: System.IO.UnmanagedMemoryStream.dll => 57
	i32 u0x3c5e5b62, ; 224: Xamarin.AndroidX.SavedState.dll => 252
	i32 u0x3cbffa41, ; 225: System.Drawing => 36
	i32 u0x3d548d92, ; 226: Microsoft.Extensions.DependencyInjection.Abstractions => 178
	i32 u0x3d5a6611, ; 227: da/Microsoft.Maui.Controls.resources.dll => 286
	i32 u0x3d7be038, ; 228: Xamarin.Google.ErrorProne.Annotations.dll => 270
	i32 u0x3dbaaf8f, ; 229: Xamarin.AndroidX.AppCompat => 203
	i32 u0x3dc84a49, ; 230: System.Drawing.Primitives.dll => 35
	i32 u0x3df150e9, ; 231: lib_Xamarin.AndroidX.Interpolator.dll.so => 229
	i32 u0x3e444eb4, ; 232: System.Linq.Expressions.dll => 59
	i32 u0x3e5c42fd, ; 233: lib_System.Reflection.TypeExtensions.dll.so => 97
	i32 u0x3eb776a1, ; 234: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 240
	i32 u0x3ebd41f6, ; 235: lib_System.Collections.dll.so => 12
	i32 u0x3ecd3024, ; 236: lib_System.Resources.Reader.dll.so => 99
	i32 u0x3eea4db8, ; 237: lib_Microsoft.Extensions.Primitives.dll.so => 183
	i32 u0x3f3e1e33, ; 238: lib_Xamarin.AndroidX.Lifecycle.Process.dll.so => 235
	i32 u0x3f9dcf8c, ; 239: GoogleGson => 174
	i32 u0x408b17f4, ; 240: System.ComponentModel.TypeConverter => 17
	i32 u0x409e66d8, ; 241: Xamarin.Kotlin.StdLib => 275
	i32 u0x41761b2c, ; 242: System => 165
	i32 u0x4232ae7b, ; 243: lib_System.Reflection.Emit.dll.so => 93
	i32 u0x42be2972, ; 244: lib_System.Text.Encodings.Web.dll.so => 137
	i32 u0x42c091c1, ; 245: lib_Xamarin.Android.Glide.GifDecoder.dll.so => 197
	i32 u0x42da3e50, ; 246: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.dll => 238
	i32 u0x43362f15, ; 247: Microsoft.Extensions.Logging.Debug => 181
	i32 u0x4393e151, ; 248: lib-th-Microsoft.Maui.Controls.resources.dll.so => 310
	i32 u0x441f18e1, ; 249: lib_System.Security.Cryptography.OpenSsl.dll.so => 124
	i32 u0x444e5c8e, ; 250: lib_System.ComponentModel.TypeConverter.dll.so => 17
	i32 u0x44549c93, ; 251: lib_System.Net.WebProxy.dll.so => 79
	i32 u0x4474042c, ; 252: lib_System.Numerics.Vectors.dll.so => 83
	i32 u0x447dc2e6, ; 253: Xamarin.AndroidX.Window => 265
	i32 u0x44845810, ; 254: lib_System.Net.Http.dll.so => 65
	i32 u0x44c3958b, ; 255: lib_System.Private.DataContractSerialization.dll.so => 86
	i32 u0x45bde382, ; 256: lib_System.Windows.dll.so => 155
	i32 u0x45c677b2, ; 257: System.Web.dll => 154
	i32 u0x460b48eb, ; 258: Xamarin.AndroidX.VectorDrawable.Animated => 261
	i32 u0x463a8801, ; 259: Xamarin.AndroidX.Navigation.Runtime.dll => 247
	i32 u0x464305ed, ; 260: fi/Microsoft.Maui.Controls.resources => 290
	i32 u0x466ae52b, ; 261: lib_System.Threading.Overlapped.dll.so => 141
	i32 u0x47a87de7, ; 262: lib_System.Resources.Writer.dll.so => 101
	i32 u0x47b79c15, ; 263: pl/Microsoft.Maui.Controls.resources.dll => 303
	i32 u0x47c7b4fa, ; 264: Xamarin.AndroidX.Arch.Core.Common => 205
	i32 u0x480a69ad, ; 265: System.Diagnostics.Process => 29
	i32 u0x48aa6be3, ; 266: System.IO.IsolatedStorage => 52
	i32 u0x48bf92c4, ; 267: lib_Xamarin.AndroidX.Collection.dll.so => 209
	i32 u0x49654709, ; 268: lib_System.Threading.Timer.dll.so => 148
	i32 u0x499b8219, ; 269: nb/Microsoft.Maui.Controls.resources.dll => 301
	i32 u0x4a0189ae, ; 270: lib-hi-Microsoft.Maui.Controls.resources.dll.so => 293
	i32 u0x4a18f6f7, ; 271: Xamarin.AndroidX.Window.Extensions.Core.Core.dll => 266
	i32 u0x4a4cd262, ; 272: Xamarin.AndroidX.Collection.Jvm.dll => 210
	i32 u0x4a8cb221, ; 273: lib_Xamarin.JSpecify.dll.so => 274
	i32 u0x4aaf6f7c, ; 274: Microsoft.Win32.Registry => 5
	i32 u0x4ae97402, ; 275: lib_Microsoft.Maui.Graphics.dll.so => 188
	i32 u0x4b275854, ; 276: Xamarin.KotlinX.Serialization.Core.Jvm => 282
	i32 u0x4b5eebe5, ; 277: Xamarin.AndroidX.Startup.StartupRuntime.dll => 256
	i32 u0x4b64b158, ; 278: Xamarin.KotlinX.Coroutines.Core.dll => 279
	i32 u0x4b863c7a, ; 279: lib_System.Private.Xml.Linq.dll.so => 88
	i32 u0x4b8a64a7, ; 280: Xamarin.AndroidX.VectorDrawable => 260
	i32 u0x4bb12d98, ; 281: lib_System.Runtime.Serialization.Xml.dll.so => 115
	i32 u0x4be46b58, ; 282: Xamarin.AndroidX.Collection.Ktx => 211
	i32 u0x4c071bea, ; 283: Xamarin.KotlinX.Coroutines.Android => 278
	i32 u0x4c3393c5, ; 284: Xamarin.AndroidX.Annotation.Jvm => 202
	i32 u0x4d0585a0, ; 285: SQLitePCLRaw.core.dll => 191
	i32 u0x4d14ee2b, ; 286: Xamarin.AndroidX.DrawerLayout.dll => 222
	i32 u0x4de0ce3b, ; 287: lib_Xamarin.AndroidX.ProfileInstaller.ProfileInstaller.dll.so => 249
	i32 u0x4e08a30b, ; 288: System.Private.DataContractSerialization => 86
	i32 u0x4e98c997, ; 289: lib_Xamarin.AndroidX.Window.Extensions.Core.Core.dll.so => 266
	i32 u0x4ed70c83, ; 290: Xamarin.AndroidX.Window.dll => 265
	i32 u0x4eed2679, ; 291: System.Linq => 62
	i32 u0x4f97822f, ; 292: System.Runtime.Serialization.Json.dll => 113
	i32 u0x50255dd9, ; 293: lib-hr-Microsoft.Maui.Controls.resources.dll.so => 294
	i32 u0x50acdfd7, ; 294: lib-ca-Microsoft.Maui.Controls.resources.dll.so => 284
	i32 u0x514d38cd, ; 295: System.IO => 58
	i32 u0x52114ed3, ; 296: Xamarin.AndroidX.SavedState => 252
	i32 u0x523dc4c1, ; 297: System.Resources.ResourceManager => 100
	i32 u0x533678bd, ; 298: lib_System.Private.CoreLib.dll.so => 173
	i32 u0x53701274, ; 299: lib_System.IO.FileSystem.Watcher.dll.so => 50
	i32 u0x53936ab4, ; 300: System.Configuration.dll => 19
	i32 u0x53cefc50, ; 301: Xamarin.AndroidX.CoordinatorLayout => 215
	i32 u0x53f80ba6, ; 302: System.Runtime.Serialization.Formatters.dll => 112
	i32 u0x5423e47b, ; 303: System.Runtime.CompilerServices.Unsafe => 102
	i32 u0x54246761, ; 304: lib_System.Diagnostics.Tools.dll.so => 32
	i32 u0x5498bac9, ; 305: lib_Microsoft.VisualBasic.dll.so => 3
	i32 u0x54ca50cb, ; 306: System.Runtime.CompilerServices.VisualC => 103
	i32 u0x557217fe, ; 307: lib_System.Numerics.dll.so => 84
	i32 u0x557b5293, ; 308: System.Runtime.Handles => 105
	i32 u0x558bc221, ; 309: Xamarin.Google.Crypto.Tink.Android.dll => 269
	i32 u0x55ab7451, ; 310: Xamarin.AndroidX.Lifecycle.Common.Jvm => 231
	i32 u0x55d10363, ; 311: System.Net.Quic.dll => 72
	i32 u0x55dfaca3, ; 312: lib_Microsoft.Win32.Primitives.dll.so => 4
	i32 u0x55e55df2, ; 313: Xamarin.AndroidX.Lifecycle.ViewModel.Android => 241
	i32 u0x568cd628, ; 314: System.Formats.Asn1.dll => 38
	i32 u0x569fcb36, ; 315: System.Diagnostics.Tools.dll => 32
	i32 u0x56c018af, ; 316: lib_System.IO.UnmanagedMemoryStream.dll.so => 57
	i32 u0x56e36530, ; 317: System.Runtime.Extensions.dll => 104
	i32 u0x56e7a7ad, ; 318: System.Net.Security.dll => 74
	i32 u0x5718a9ef, ; 319: System.Collections.Immutable.dll => 9
	i32 u0x57201017, ; 320: System.Security.Cryptography.OpenSsl => 124
	i32 u0x57261233, ; 321: System.IO.Compression.dll => 46
	i32 u0x57924923, ; 322: Xamarin.AndroidX.AppCompat.AppCompatResources => 204
	i32 u0x57a5e912, ; 323: Microsoft.Extensions.Primitives => 183
	i32 u0x57d4e229, ; 324: StokBarangMAUI.dll => 0
	i32 u0x5833866d, ; 325: System.Collections.Immutable => 9
	i32 u0x583e844f, ; 326: System.IO.Compression.Brotli.dll => 43
	i32 u0x58a57897, ; 327: Microsoft.Win32.Primitives => 4
	i32 u0x58cffa99, ; 328: Xamarin.AndroidX.SavedState.SavedState.Ktx.dll => 253
	i32 u0x58fd6613, ; 329: hi/Microsoft.Maui.Controls.resources.dll => 293
	i32 u0x596b5b3a, ; 330: lib_System.Drawing.Primitives.dll.so => 35
	i32 u0x5a48cf6c, ; 331: el/Microsoft.Maui.Controls.resources.dll => 288
	i32 u0x5b9331b6, ; 332: System.Diagnostics.TextWriterTraceListener => 31
	i32 u0x5be451c7, ; 333: lib_Xamarin.AndroidX.Browser.dll.so => 207
	i32 u0x5bf8ca0f, ; 334: System.Text.RegularExpressions.dll => 139
	i32 u0x5bfdbb43, ; 335: System.Reflection.Emit.dll => 93
	i32 u0x5c680b40, ; 336: System.Reflection.Extensions.dll => 94
	i32 u0x5c7be408, ; 337: sk/Microsoft.Maui.Controls.resources.dll => 308
	i32 u0x5cabc9a4, ; 338: fr/Microsoft.Maui.Controls.resources => 291
	i32 u0x5d552ab7, ; 339: System.IO.FileSystem.Primitives => 49
	i32 u0x5d5a6c40, ; 340: System.Threading.Tasks.Dataflow.dll => 142
	i32 u0x5dccd455, ; 341: System.Runtime.Serialization.Json => 113
	i32 u0x5e0b6fdc, ; 342: Xamarin.KotlinX.Serialization.Core.Jvm.dll => 282
	i32 u0x5e2d7514, ; 343: System.Threading.Overlapped => 141
	i32 u0x5e2e3abe, ; 344: lib_Microsoft.VisualBasic.Core.dll.so => 2
	i32 u0x5e33306d, ; 345: sv/Microsoft.Maui.Controls.resources => 309
	i32 u0x5e7321d2, ; 346: lib_System.ComponentModel.Primitives.dll.so => 16
	i32 u0x5ed5f779, ; 347: zh-Hant/Microsoft.Maui.Controls.resources => 316
	i32 u0x5ef2ee25, ; 348: System.Runtime.Serialization.dll => 116
	i32 u0x5f3ec4dd, ; 349: Xamarin.Google.ErrorProne.Annotations => 270
	i32 u0x5f6f0b5b, ; 350: System.Xml.Serialization => 158
	i32 u0x5fa7b851, ; 351: System.Net.WebClient => 77
	i32 u0x6078995d, ; 352: System.Net.WebSockets.Client.dll => 80
	i32 u0x60892624, ; 353: lib_System.Formats.Tar.dll.so => 39
	i32 u0x60b0136a, ; 354: Xamarin.AndroidX.Loader.dll => 244
	i32 u0x60b33958, ; 355: System.Dynamic.Runtime => 37
	i32 u0x60d97228, ; 356: Xamarin.AndroidX.ViewPager2 => 264
	i32 u0x60ec189c, ; 357: lib_Xamarin.AndroidX.Arch.Core.Runtime.dll.so => 206
	i32 u0x6176eff7, ; 358: Xamarin.AndroidX.Emoji2.ViewsHelper => 225
	i32 u0x6188ba7e, ; 359: Xamarin.AndroidX.CursorAdapter => 219
	i32 u0x61b9038d, ; 360: System.Net.Http.dll => 65
	i32 u0x61c036ca, ; 361: System.Text.RegularExpressions => 139
	i32 u0x61d59e0e, ; 362: System.ComponentModel.EventBasedAsync.dll => 15
	i32 u0x62021776, ; 363: lib_System.IO.Compression.dll.so => 46
	i32 u0x620a8774, ; 364: lib_System.Xml.ReaderWriter.dll.so => 157
	i32 u0x625755ef, ; 365: lib_WindowsBase.dll.so => 166
	i32 u0x62c6282e, ; 366: System.Runtime => 117
	i32 u0x62cec1a2, ; 367: lib_Xamarin.KotlinX.Coroutines.Core.Jvm.dll.so => 280
	i32 u0x62d6c1e4, ; 368: Xamarin.AndroidX.Tracing.Tracing.dll => 258
	i32 u0x62d6ea10, ; 369: Xamarin.Google.Android.Material.dll => 267
	i32 u0x638b1991, ; 370: Xamarin.AndroidX.ConstraintLayout => 213
	i32 u0x63dee9da, ; 371: System.IO.FileSystem.DriveInfo.dll => 48
	i32 u0x63fca3d0, ; 372: System.Net.Primitives.dll => 71
	i32 u0x640c0103, ; 373: System.Net.WebSockets => 81
	i32 u0x641979dd, ; 374: Xamarin.JSpecify => 274
	i32 u0x641f3e5a, ; 375: System.Security.Cryptography => 127
	i32 u0x64d1e4f5, ; 376: System.Reflection.Metadata => 95
	i32 u0x6525abc9, ; 377: System.Security.Cryptography.Csp => 122
	i32 u0x654b1498, ; 378: lib_System.Transactions.Local.dll.so => 150
	i32 u0x656b7698, ; 379: System.Diagnostics.Debug.dll => 26
	i32 u0x660284a1, ; 380: SQLitePCLRaw.lib.e_sqlite3.android => 192
	i32 u0x6670b12e, ; 381: lib_System.Security.AccessControl.dll.so => 118
	i32 u0x66888819, ; 382: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx => 234
	i32 u0x66e27484, ; 383: System.Reflection.dll => 98
	i32 u0x66ffb0f8, ; 384: System.Diagnostics.FileVersionInfo.dll => 28
	i32 u0x6715dc86, ; 385: Xamarin.AndroidX.CardView.dll => 208
	i32 u0x67577fe1, ; 386: lib_System.Runtime.CompilerServices.VisualC.dll.so => 103
	i32 u0x677cd287, ; 387: ro/Microsoft.Maui.Controls.resources.dll => 306
	i32 u0x67fe8db2, ; 388: System.Transactions.Local.dll => 150
	i32 u0x68139a0d, ; 389: System.IO.Pipelines.dll => 54
	i32 u0x6816ab6a, ; 390: Mono.Android.Export => 170
	i32 u0x6853a83d, ; 391: Microsoft.Win32.Primitives.dll => 4
	i32 u0x68cc9d1e, ; 392: System.Resources.Reader.dll => 99
	i32 u0x68f61ae4, ; 393: lib_System.Formats.Asn1.dll.so => 38
	i32 u0x690d4b7d, ; 394: lib-zh-Hant-Microsoft.Maui.Controls.resources.dll.so => 316
	i32 u0x69239124, ; 395: System.Diagnostics.TraceSource.dll => 33
	i32 u0x693efa35, ; 396: lib_System.Net.WebHeaderCollection.dll.so => 78
	i32 u0x6942234e, ; 397: System.Reflection.Extensions => 94
	i32 u0x6947f945, ; 398: Xamarin.AndroidX.SwipeRefreshLayout => 257
	i32 u0x6988f147, ; 399: Microsoft.Extensions.Logging.dll => 179
	i32 u0x69ae5451, ; 400: lib_System.Runtime.InteropServices.JavaScript.dll.so => 106
	i32 u0x69dc03cc, ; 401: System.Core.dll => 21
	i32 u0x69ec0683, ; 402: System.Globalization.Extensions.dll => 41
	i32 u0x69f4f41d, ; 403: lib_Xamarin.AndroidX.AppCompat.dll.so => 203
	i32 u0x6a216153, ; 404: Mono.Android.Runtime.dll => 171
	i32 u0x6a539b49, ; 405: lib_System.Runtime.Extensions.dll.so => 104
	i32 u0x6a96652d, ; 406: Xamarin.AndroidX.Fragment => 227
	i32 u0x6afaf338, ; 407: lib_System.Threading.dll.so => 149
	i32 u0x6b645ada, ; 408: lib-fr-Microsoft.Maui.Controls.resources.dll.so => 291
	i32 u0x6bcd3296, ; 409: Xamarin.AndroidX.Loader => 244
	i32 u0x6be1e423, ; 410: nb/Microsoft.Maui.Controls.resources => 301
	i32 u0x6c111525, ; 411: Xamarin.Kotlin.StdLib.dll => 275
	i32 u0x6c13413e, ; 412: Xamarin.Google.Android.Material => 267
	i32 u0x6c5562e0, ; 413: lib_Xamarin.KotlinX.Coroutines.Core.dll.so => 279
	i32 u0x6c652ce8, ; 414: Xamarin.AndroidX.Navigation.UI.dll => 248
	i32 u0x6c687fa7, ; 415: Microsoft.VisualBasic.Core => 2
	i32 u0x6c96614d, ; 416: hu/Microsoft.Maui.Controls.resources => 295
	i32 u0x6cbab720, ; 417: System.Text.Encoding.Extensions => 135
	i32 u0x6cc30c8c, ; 418: System.Runtime.Serialization.Formatters => 112
	i32 u0x6cf3d432, ; 419: lib_Xamarin.AndroidX.VersionedParcelable.dll.so => 262
	i32 u0x6cff90ba, ; 420: Microsoft.Extensions.Logging.Abstractions.dll => 180
	i32 u0x6dcaebf7, ; 421: uk/Microsoft.Maui.Controls.resources.dll => 312
	i32 u0x6e1ed932, ; 422: Xamarin.Android.Glide.Annotations => 195
	i32 u0x6ec71a65, ; 423: System.Linq.Expressions => 59
	i32 u0x6f7a29e4, ; 424: System.Reflection.Primitives => 96
	i32 u0x6fab02f2, ; 425: lib_Xamarin.AndroidX.ConstraintLayout.dll.so => 213
	i32 u0x7009e4c3, ; 426: System.Formats.Tar.dll => 39
	i32 u0x705fa726, ; 427: Xamarin.AndroidX.Arch.Core.Runtime.dll => 206
	i32 u0x7070c6c0, ; 428: lib-zh-Hans-Microsoft.Maui.Controls.resources.dll.so => 315
	i32 u0x70972b6d, ; 429: System.Diagnostics.Contracts => 25
	i32 u0x70a66bdd, ; 430: System.Reflection.Metadata.dll => 95
	i32 u0x7124cf39, ; 431: System.Reflection.DispatchProxy => 90
	i32 u0x71490522, ; 432: System.Resources.ResourceManager.dll => 100
	i32 u0x71dc7c8b, ; 433: System.Collections.NonGeneric.dll => 10
	i32 u0x72fcebde, ; 434: lib_Xamarin.AndroidX.AppCompat.AppCompatResources.dll.so => 204
	i32 u0x731dd955, ; 435: lib_Mono.Android.dll.so => 172
	i32 u0x73674b00, ; 436: lib_SQLitePCLRaw.provider.e_sqlite3.dll.so => 193
	i32 u0x739bd4a8, ; 437: System.Private.Xml.Linq => 88
	i32 u0x73b20433, ; 438: lib_System.IO.FileSystem.Primitives.dll.so => 49
	i32 u0x73fbecbe, ; 439: lib_System.Memory.dll.so => 63
	i32 u0x74126030, ; 440: lib_System.Net.WebClient.dll.so => 77
	i32 u0x74a1c5bb, ; 441: System.Resources.Writer => 101
	i32 u0x74d743bf, ; 442: ja/Microsoft.Maui.Controls.resources => 298
	i32 u0x74eee4ef, ; 443: Xamarin.AndroidX.Security.SecurityCrypto.dll => 254
	i32 u0x75533a5e, ; 444: Microsoft.Extensions.Configuration.dll => 175
	i32 u0x7593c33f, ; 445: lib_System.IO.FileSystem.AccessControl.dll.so => 47
	i32 u0x765c50a4, ; 446: Xamarin.Android.Glide.GifDecoder => 197
	i32 u0x77ec19b4, ; 447: System.Buffers.dll => 7
	i32 u0x781074ce, ; 448: hr/Microsoft.Maui.Controls.resources => 294
	i32 u0x784d3e49, ; 449: lib_System.Net.dll.so => 82
	i32 u0x785e97f1, ; 450: Xamarin.AndroidX.Lifecycle.ViewModel => 240
	i32 u0x78b622b1, ; 451: ar/Microsoft.Maui.Controls.resources.dll => 283
	i32 u0x790376c9, ; 452: lib_Xamarin.AndroidX.Annotation.Experimental.dll.so => 201
	i32 u0x791a414b, ; 453: Xamarin.Android.Glide => 194
	i32 u0x7970be4f, ; 454: lib-he-Microsoft.Maui.Controls.resources.dll.so => 292
	i32 u0x79d00016, ; 455: it/Microsoft.Maui.Controls.resources => 297
	i32 u0x79eb68ee, ; 456: System.Private.Xml => 89
	i32 u0x7a80bd4e, ; 457: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 233
	i32 u0x7aca0819, ; 458: System.Windows.dll => 155
	i32 u0x7b350579, ; 459: lib__Microsoft.Android.Resource.Designer.dll.so => 317
	i32 u0x7b473a37, ; 460: lib_Xamarin.AndroidX.Fragment.Ktx.dll.so => 228
	i32 u0x7b6f419e, ; 461: System.Diagnostics.TraceSource => 33
	i32 u0x7b8f6ff7, ; 462: lib_System.Runtime.Serialization.Json.dll.so => 113
	i32 u0x7bf8cdab, ; 463: System.Runtime.dll => 117
	i32 u0x7c51ebd4, ; 464: lib_System.Net.HttpListener.dll.so => 66
	i32 u0x7c9bf920, ; 465: System.Numerics.Vectors => 83
	i32 u0x7d065c82, ; 466: lib_Xamarin.Google.ErrorProne.TypeAnnotations.dll.so => 271
	i32 u0x7d603cde, ; 467: SQLitePCLRaw.provider.e_sqlite3.dll => 193
	i32 u0x7d702d52, ; 468: lib_System.Text.Encoding.dll.so => 136
	i32 u0x7ec9ffe9, ; 469: System.Console => 20
	i32 u0x7fb38cd2, ; 470: System.Collections.Specialized => 11
	i32 u0x7fc7a41e, ; 471: System.Xml.XmlSerializer.dll => 163
	i32 u0x7fd90a71, ; 472: lib_System.Text.Encoding.CodePages.dll.so => 134
	i32 u0x7fdcdc37, ; 473: lib-ko-Microsoft.Maui.Controls.resources.dll.so => 299
	i32 u0x7ff65cf5, ; 474: Microsoft.VisualBasic.dll => 3
	i32 u0x802a7166, ; 475: lib_System.Diagnostics.FileVersionInfo.dll.so => 28
	i32 u0x8030853e, ; 476: ko/Microsoft.Maui.Controls.resources => 299
	i32 u0x8044e1bd, ; 477: lib-ms-Microsoft.Maui.Controls.resources.dll.so => 300
	i32 u0x8081c489, ; 478: lib_Jsr305Binding.dll.so => 268
	i32 u0x80bd55ad, ; 479: Microsoft.Maui => 186
	i32 u0x80f2f56e, ; 480: lib_System.Runtime.Serialization.Formatters.dll.so => 112
	i32 u0x810c11c2, ; 481: ro/Microsoft.Maui.Controls.resources => 306
	i32 u0x8115bdf3, ; 482: lib_System.Resources.ResourceManager.dll.so => 100
	i32 u0x816751d8, ; 483: lib_System.Diagnostics.DiagnosticSource.dll.so => 27
	i32 u0x81a110ae, ; 484: lib_System.ComponentModel.EventBasedAsync.dll.so => 15
	i32 u0x820d22b3, ; 485: Microsoft.Extensions.Options.dll => 182
	i32 u0x82364da2, ; 486: lib_System.Buffers.dll.so => 7
	i32 u0x82a8237c, ; 487: Microsoft.Extensions.Logging.Abstractions => 180
	i32 u0x82b6c85e, ; 488: System.ObjectModel.dll => 85
	i32 u0x82bb5429, ; 489: lib_System.Linq.Expressions.dll.so => 59
	i32 u0x82c1cf3e, ; 490: lib_System.Net.Quic.dll.so => 72
	i32 u0x832ec206, ; 491: lib_System.Diagnostics.StackTrace.dll.so => 30
	i32 u0x83323b38, ; 492: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 280
	i32 u0x8334206b, ; 493: System.Net.Http => 65
	i32 u0x842e93b2, ; 494: Xamarin.AndroidX.VectorDrawable.Animated.dll => 261
	i32 u0x8471e4ec, ; 495: System.Threading.Tasks.Parallel => 144
	i32 u0x857e4dd2, ; 496: lib_System.Net.WebSockets.dll.so => 81
	i32 u0x8628f1a4, ; 497: lib-ru-Microsoft.Maui.Controls.resources.dll.so => 307
	i32 u0x863c6ac5, ; 498: System.Xml.Serialization.dll => 158
	i32 u0x867c9c52, ; 499: System.Globalization.Extensions => 41
	i32 u0x86b0fd78, ; 500: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll.so => 242
	i32 u0x86bba59b, ; 501: lib_Microsoft.Maui.Controls.dll.so => 184
	i32 u0x8702d9a2, ; 502: System.Security.AccessControl.dll => 118
	i32 u0x871c9c1b, ; 503: Microsoft.Extensions.Configuration.Abstractions => 176
	i32 u0x872eeb7b, ; 504: Xamarin.Android.Glide.DiskLruCache.dll => 196
	i32 u0x875633cc, ; 505: fr/Microsoft.Maui.Controls.resources.dll => 291
	i32 u0x87a1a22b, ; 506: lib-it-Microsoft.Maui.Controls.resources.dll.so => 297
	i32 u0x87e25095, ; 507: Xamarin.AndroidX.RecyclerView.dll => 250
	i32 u0x87e7fdbb, ; 508: lib-nl-Microsoft.Maui.Controls.resources.dll.so => 302
	i32 u0x881f94da, ; 509: lib_netstandard.dll.so => 168
	i32 u0x8873eb17, ; 510: th/Microsoft.Maui.Controls.resources => 310
	i32 u0x887ae6a1, ; 511: lib_Xamarin.AndroidX.Lifecycle.Runtime.Android.dll.so => 237
	i32 u0x88acefcd, ; 512: System.ServiceModel.Web.dll => 132
	i32 u0x88d8bfaa, ; 513: System.Net.Sockets => 76
	i32 u0x88ed6f27, ; 514: lib_SQLitePCLRaw.batteries_v2.dll.so => 190
	i32 u0x88ffe49e, ; 515: System.Net.Mail => 67
	i32 u0x896b7878, ; 516: System.Private.CoreLib.dll => 173
	i32 u0x8a068af2, ; 517: Xamarin.AndroidX.Annotation.dll => 200
	i32 u0x8a52059a, ; 518: System.Threading.Tasks.Parallel.dll => 144
	i32 u0x8b804dbf, ; 519: System.Runtime.InteropServices.RuntimeInformation.dll => 107
	i32 u0x8bbaa2cd, ; 520: System.ValueTuple => 152
	i32 u0x8c20c628, ; 521: lib-fi-Microsoft.Maui.Controls.resources.dll.so => 290
	i32 u0x8c20f140, ; 522: lib_System.Console.dll.so => 20
	i32 u0x8c40e0db, ; 523: System.Net.Primitives => 71
	i32 u0x8c93dffb, ; 524: lib_SQLite-net.dll.so => 189
	i32 u0x8d19e4a2, ; 525: lib_Xamarin.AndroidX.Lifecycle.LiveData.dll.so => 232
	i32 u0x8d24e767, ; 526: System.Xml.ReaderWriter.dll => 157
	i32 u0x8d3fac99, ; 527: tr/Microsoft.Maui.Controls.resources => 311
	i32 u0x8d52b2e2, ; 528: Microsoft.Extensions.Configuration => 175
	i32 u0x8d52d3de, ; 529: lib_System.Threading.Tasks.dll.so => 145
	i32 u0x8dc6dbce, ; 530: System.Security.Cryptography.Csp.dll => 122
	i32 u0x8dcb0101, ; 531: lib_Xamarin.AndroidX.Navigation.Fragment.dll.so => 246
	i32 u0x8e02310f, ; 532: lib-ar-Microsoft.Maui.Controls.resources.dll.so => 283
	i32 u0x8e114655, ; 533: System.Security.Principal.Windows.dll => 128
	i32 u0x8f24faee, ; 534: System.Web.HttpUtility => 153
	i32 u0x8f41c524, ; 535: Xamarin.AndroidX.Emoji2.dll => 224
	i32 u0x8f4e087a, ; 536: lib_System.Web.dll.so => 154
	i32 u0x8f8c64e2, ; 537: lib_System.Private.Xml.dll.so => 89
	i32 u0x905355ed, ; 538: System.Threading.Tasks.Dataflow => 142
	i32 u0x905caa9d, ; 539: nl/Microsoft.Maui.Controls.resources => 302
	i32 u0x906d466b, ; 540: Xamarin.AndroidX.Collection.Ktx.dll => 211
	i32 u0x90e50509, ; 541: lib_System.Reflection.Primitives.dll.so => 96
	i32 u0x911615a7, ; 542: lib_Xamarin.AndroidX.Fragment.dll.so => 227
	i32 u0x912896e5, ; 543: System.Console.dll => 20
	i32 u0x9130f5e7, ; 544: System.ComponentModel.DataAnnotations.dll => 14
	i32 u0x91abdf3a, ; 545: lib_Xamarin.AndroidX.Startup.StartupRuntime.dll.so => 256
	i32 u0x924edee6, ; 546: System.Text.Encoding.dll => 136
	i32 u0x928c75ca, ; 547: System.Net.Sockets.dll => 76
	i32 u0x92916334, ; 548: System.Linq.Parallel.dll => 60
	i32 u0x92f11675, ; 549: SQLitePCLRaw.batteries_v2.dll => 190
	i32 u0x92f50938, ; 550: Xamarin.AndroidX.ConstraintLayout.Core.dll => 214
	i32 u0x93554fdc, ; 551: netstandard.dll => 168
	i32 u0x93634cc0, ; 552: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx.dll.so => 234
	i32 u0x93918882, ; 553: Java.Interop.dll => 169
	i32 u0x93dba8a1, ; 554: Microsoft.Maui.Controls => 184
	i32 u0x940d5c2f, ; 555: System.ComponentModel.EventBasedAsync => 15
	i32 u0x94147f61, ; 556: System.Net.ServicePoint => 75
	i32 u0x9438d78e, ; 557: lib_System.Text.Json.dll.so => 138
	i32 u0x9469ba86, ; 558: lib_Xamarin.AndroidX.Lifecycle.Runtime.dll.so => 236
	i32 u0x94798bc5, ; 559: System.AppContext.dll => 6
	i32 u0x94a1db18, ; 560: lib-id-Microsoft.Maui.Controls.resources.dll.so => 296
	i32 u0x94fad8e5, ; 561: lib_Xamarin.AndroidX.Activity.Ktx.dll.so => 199
	i32 u0x95178668, ; 562: System.Data.DataSetExtensions => 23
	i32 u0x955cf248, ; 563: Xamarin.AndroidX.Lifecycle.Runtime.dll => 236
	i32 u0x9593ae7f, ; 564: lib_Xamarin.AndroidX.SavedState.dll.so => 252
	i32 u0x963ac2da, ; 565: sk/Microsoft.Maui.Controls.resources => 308
	i32 u0x9659e17c, ; 566: Xamarin.Android.Glide.dll => 194
	i32 u0x96bea474, ; 567: lib_Microsoft.Maui.Controls.Xaml.dll.so => 185
	i32 u0x974b89a2, ; 568: System.Reflection.Emit.Lightweight.dll => 92
	i32 u0x98ba5a04, ; 569: Microsoft.CSharp => 1
	i32 u0x9930ee42, ; 570: System.Text.Encodings.Web => 137
	i32 u0x999dcf0d, ; 571: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android => 239
	i32 u0x99e2e424, ; 572: Xamarin.AndroidX.Lifecycle.Runtime.Ktx => 238
	i32 u0x99e370f2, ; 573: Xamarin.AndroidX.VectorDrawable.dll => 260
	i32 u0x9a1756ac, ; 574: System.Text.Encoding.Extensions.dll => 135
	i32 u0x9a20430d, ; 575: System.Net.Ping => 70
	i32 u0x9a5a3337, ; 576: System.Threading.ThreadPool => 147
	i32 u0x9b24ab96, ; 577: lib_System.Runtime.Serialization.Primitives.dll.so => 114
	i32 u0x9b500441, ; 578: Xamarin.KotlinX.Coroutines.Core.Jvm => 280
	i32 u0x9b5e5b1c, ; 579: lib_System.Diagnostics.Contracts.dll.so => 25
	i32 u0x9be14c08, ; 580: Xamarin.AndroidX.Fragment.Ktx => 228
	i32 u0x9bf052c1, ; 581: Microsoft.Extensions.Logging.Debug.dll => 181
	i32 u0x9bfe3a41, ; 582: System.Private.Xml.dll => 89
	i32 u0x9c165ff9, ; 583: System.Reflection.TypeExtensions.dll => 97
	i32 u0x9c375496, ; 584: Xamarin.AndroidX.CursorAdapter.dll => 219
	i32 u0x9c70e6c9, ; 585: Xamarin.AndroidX.DynamicAnimation => 223
	i32 u0x9c96ac4c, ; 586: lib_Xamarin.AndroidX.Navigation.UI.dll.so => 248
	i32 u0x9c97ad4a, ; 587: System.Diagnostics.TextWriterTraceListener.dll => 31
	i32 u0x9cc03a58, ; 588: System.IO.Compression.ZipFile.dll => 45
	i32 u0x9cd341b2, ; 589: lib_System.Threading.Tasks.Parallel.dll.so => 144
	i32 u0x9cf12c56, ; 590: Xamarin.AndroidX.Lifecycle.LiveData => 232
	i32 u0x9e78dac1, ; 591: lib_Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll.so => 243
	i32 u0x9ec022c0, ; 592: Xamarin.Android.Glide.DiskLruCache => 196
	i32 u0x9ec4cf01, ; 593: System.Runtime.Loader => 110
	i32 u0x9ecf752a, ; 594: System.Xml.XDocument.dll => 159
	i32 u0x9ee22cc0, ; 595: System.Drawing.Primitives => 35
	i32 u0x9f3b757e, ; 596: Xamarin.KotlinX.Coroutines.Core => 279
	i32 u0x9f7ea921, ; 597: lib_System.Runtime.InteropServices.dll.so => 108
	i32 u0x9f8c6f40, ; 598: System.Data.Common.dll => 22
	i32 u0xa026a50c, ; 599: System.Runtime.Serialization.Xml.dll => 115
	i32 u0xa090e36a, ; 600: System.IO.dll => 58
	i32 u0xa0fb56af, ; 601: lib_System.Text.RegularExpressions.dll.so => 139
	i32 u0xa0ff7514, ; 602: Xamarin.AndroidX.Tracing.Tracing => 258
	i32 u0xa1d8b647, ; 603: System.Threading.Tasks.dll => 145
	i32 u0xa1fd7d9f, ; 604: System.Security.Claims => 119
	i32 u0xa21f5a1f, ; 605: System.Security.Cryptography.Cng.dll => 121
	i32 u0xa25c90e5, ; 606: lib_Xamarin.AndroidX.Core.dll.so => 216
	i32 u0xa262a30f, ; 607: System.Runtime.Numerics.dll => 111
	i32 u0xa2ce8457, ; 608: lib-es-Microsoft.Maui.Controls.resources.dll.so => 289
	i32 u0xa2e0939b, ; 609: Xamarin.AndroidX.Activity => 198
	i32 u0xa30769e5, ; 610: System.Threading.Channels => 140
	i32 u0xa32eb6f0, ; 611: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 204
	i32 u0xa35f8f92, ; 612: System.IO.Pipes.AccessControl => 55
	i32 u0xa3c818c7, ; 613: lib_System.Net.WebSockets.Client.dll.so => 80
	i32 u0xa3cc7fa7, ; 614: System.Runtime.InteropServices.JavaScript => 106
	i32 u0xa4672f3b, ; 615: Microsoft.Maui.Controls.Xaml => 185
	i32 u0xa493aa02, ; 616: lib_System.Collections.Concurrent.dll.so => 8
	i32 u0xa4caf7a7, ; 617: Microsoft.Maui.dll => 186
	i32 u0xa4d4aaf8, ; 618: lib_System.Security.Cryptography.Cng.dll.so => 121
	i32 u0xa4db22c6, ; 619: System.Text.Encoding.CodePages.dll => 134
	i32 u0xa4e79dfd, ; 620: Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll => 241
	i32 u0xa522693c, ; 621: Xamarin.Jetbrains.Annotations.dll => 273
	i32 u0xa52ac270, ; 622: lib_Xamarin.AndroidX.Window.dll.so => 265
	i32 u0xa553c739, ; 623: lib_System.ValueTuple.dll.so => 152
	i32 u0xa5a0a402, ; 624: Xamarin.AndroidX.ViewPager.dll => 263
	i32 u0xa5b3182d, ; 625: Xamarin.AndroidX.ResourceInspection.Annotation.dll => 251
	i32 u0xa5b67c07, ; 626: Xamarin.AndroidX.Lifecycle.Common.Jvm.dll => 231
	i32 u0xa5c5753c, ; 627: lib_System.Collections.Immutable.dll.so => 9
	i32 u0xa5ea80d9, ; 628: lib_Xamarin.Android.Glide.Annotations.dll.so => 195
	i32 u0xa6133c7f, ; 629: lib_System.IO.FileSystem.DriveInfo.dll.so => 48
	i32 u0xa630ecdd, ; 630: Xamarin.AndroidX.Fragment.Ktx.dll => 228
	i32 u0xa668c988, ; 631: lib_System.Net.NameResolution.dll.so => 68
	i32 u0xa685bd50, ; 632: Xamarin.Google.ErrorProne.TypeAnnotations.dll => 271
	i32 u0xa7008e0b, ; 633: Microsoft.Maui.Graphics => 188
	i32 u0xa7042ae3, ; 634: uk/Microsoft.Maui.Controls.resources => 312
	i32 u0xa715dd7e, ; 635: System.Xml.XPath.XDocument.dll => 160
	i32 u0xa741ef0b, ; 636: es/Microsoft.Maui.Controls.resources.dll => 289
	i32 u0xa744f665, ; 637: lib_Xamarin.AndroidX.Navigation.Runtime.dll.so => 247
	i32 u0xa78103bc, ; 638: Xamarin.AndroidX.CoordinatorLayout.dll => 215
	i32 u0xa8032c67, ; 639: lib_Microsoft.Win32.Registry.dll.so => 5
	i32 u0xa80db4e1, ; 640: System.Xml.dll => 164
	i32 u0xa81b119f, ; 641: lib_System.Security.Cryptography.dll.so => 127
	i32 u0xa8282c09, ; 642: System.ServiceProcess.dll => 133
	i32 u0xa8298928, ; 643: Xamarin.AndroidX.ResourceInspection.Annotation => 251
	i32 u0xa85a7b6c, ; 644: System.Xml.XmlDocument => 162
	i32 u0xa8c61dcb, ; 645: nl/Microsoft.Maui.Controls.resources.dll => 302
	i32 u0xa9379a4f, ; 646: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll => 242
	i32 u0xa9d96f9b, ; 647: System.Threading.Overlapped.dll => 141
	i32 u0xaa107fc4, ; 648: Xamarin.AndroidX.ViewPager => 263
	i32 u0xaa2b531f, ; 649: lib_System.Globalization.dll.so => 42
	i32 u0xaa36a797, ; 650: Xamarin.AndroidX.Transition => 259
	i32 u0xaa4e51ff, ; 651: el/Microsoft.Maui.Controls.resources => 288
	i32 u0xaa88e550, ; 652: Mono.Android.Export.dll => 170
	i32 u0xaa8a4878, ; 653: Microsoft.Maui.Essentials => 187
	i32 u0xab123e9a, ; 654: Xamarin.AndroidX.Activity.Ktx.dll => 199
	i32 u0xab5f85c3, ; 655: Jsr305Binding.dll => 268
	i32 u0xab606289, ; 656: System.Globalization.Calendars.dll => 40
	i32 u0xabbc23e8, ; 657: lib_Xamarin.KotlinX.Serialization.Core.Jvm.dll.so => 282
	i32 u0xabdea79a, ; 658: ru/Microsoft.Maui.Controls.resources => 307
	i32 u0xabf58099, ; 659: Xamarin.AndroidX.ExifInterface => 226
	i32 u0xac1dd496, ; 660: System.Net.dll => 82
	i32 u0xacd6baa9, ; 661: System.IO.UnmanagedMemoryStream => 57
	i32 u0xace3f9b4, ; 662: System.Dynamic.Runtime.dll => 37
	i32 u0xace7ba82, ; 663: lib_System.Security.Principal.Windows.dll.so => 128
	i32 u0xacf080de, ; 664: System.Reflection => 98
	i32 u0xad2a79b6, ; 665: mscorlib.dll => 167
	i32 u0xad6f1e8a, ; 666: System.Private.CoreLib => 173
	i32 u0xad90894d, ; 667: lib_Xamarin.KotlinX.Serialization.Core.dll.so => 281
	i32 u0xaddb6d38, ; 668: Xamarin.AndroidX.ViewPager2.dll => 264
	i32 u0xae037813, ; 669: System.Numerics.Vectors.dll => 83
	i32 u0xae1ce33f, ; 670: Xamarin.AndroidX.Annotation.Experimental.dll => 201
	i32 u0xaeb2d8a5, ; 671: lib_Microsoft.Extensions.Options.dll.so => 182
	i32 u0xaf06273c, ; 672: System.Resources.Reader => 99
	i32 u0xaf3a6b91, ; 673: lib_System.Diagnostics.Debug.dll.so => 26
	i32 u0xaf4af872, ; 674: System.Diagnostics.StackTrace.dll => 30
	i32 u0xaf624531, ; 675: System.Xml.XPath.XDocument => 160
	i32 u0xaf8b1081, ; 676: lib_Xamarin.AndroidX.SavedState.SavedState.Ktx.dll.so => 253
	i32 u0xb0682092, ; 677: System.ComponentModel.dll => 18
	i32 u0xb0ed41f3, ; 678: System.Security.Principal.Windows => 128
	i32 u0xb128f886, ; 679: System.Security.Cryptography.Algorithms.dll => 120
	i32 u0xb18af942, ; 680: Xamarin.AndroidX.DrawerLayout => 222
	i32 u0xb1a434a2, ; 681: lib_System.Xml.Linq.dll.so => 156
	i32 u0xb1a7d210, ; 682: lib_Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android.dll.so => 239
	i32 u0xb21220a3, ; 683: Xamarin.AndroidX.Security.SecurityCrypto => 254
	i32 u0xb223fa8c, ; 684: lib-cs-Microsoft.Maui.Controls.resources.dll.so => 285
	i32 u0xb28cab85, ; 685: lib_Xamarin.Android.Glide.DiskLruCache.dll.so => 196
	i32 u0xb294d40b, ; 686: lib_System.Net.Ping.dll.so => 70
	i32 u0xb2a03f9f, ; 687: Xamarin.AndroidX.Lifecycle.Process.dll => 235
	i32 u0xb3d3821c, ; 688: Xamarin.AndroidX.Startup.StartupRuntime => 256
	i32 u0xb434b64b, ; 689: WindowsBase.dll => 166
	i32 u0xb514b305, ; 690: _Microsoft.Android.Resource.Designer.dll => 317
	i32 u0xb58d85d9, ; 691: lib_System.Runtime.Handles.dll.so => 105
	i32 u0xb62a9ccb, ; 692: Xamarin.AndroidX.SavedState.SavedState.Ktx => 253
	i32 u0xb63fa9f0, ; 693: Xamarin.AndroidX.Navigation.Common => 245
	i32 u0xb6490b5e, ; 694: lib_Mono.Android.Export.dll.so => 170
	i32 u0xb65adef9, ; 695: Mono.Android.Runtime => 171
	i32 u0xb660be12, ; 696: System.ComponentModel.Primitives => 16
	i32 u0xb6a153b2, ; 697: lib_Xamarin.AndroidX.ViewPager2.dll.so => 264
	i32 u0xb70c6fb4, ; 698: lib_Xamarin.AndroidX.VectorDrawable.dll.so => 260
	i32 u0xb755818f, ; 699: System.Threading.Tasks => 145
	i32 u0xb76be845, ; 700: hu/Microsoft.Maui.Controls.resources.dll => 295
	i32 u0xb7e7c341, ; 701: lib_System.Globalization.Calendars.dll.so => 40
	i32 u0xb838e2b0, ; 702: System.Security.Cryptography.X509Certificates.dll => 126
	i32 u0xb8c22b7f, ; 703: System.Security.Claims.dll => 119
	i32 u0xb8fd311b, ; 704: System.Formats.Asn1 => 38
	i32 u0xb979e222, ; 705: System.Runtime.Serialization => 116
	i32 u0xba0dbf1c, ; 706: System.IO.FileSystem.AccessControl.dll => 47
	i32 u0xba4127cb, ; 707: System.Threading.Tasks.Extensions => 143
	i32 u0xbaa520e7, ; 708: lib_System.ObjectModel.dll.so => 85
	i32 u0xbab301d1, ; 709: System.Security.AccessControl => 118
	i32 u0xbb95ee37, ; 710: System.Diagnostics.Tracing.dll => 34
	i32 u0xbba64c02, ; 711: GoogleGson.dll => 174
	i32 u0xbc4c6465, ; 712: System.Reflection.Primitives.dll => 96
	i32 u0xbc652da7, ; 713: System.IO.MemoryMappedFiles => 53
	i32 u0xbc98c93d, ; 714: lib_Xamarin.AndroidX.Collection.Jvm.dll.so => 210
	i32 u0xbcc610a0, ; 715: lib_System.Reflection.Metadata.dll.so => 95
	i32 u0xbd113355, ; 716: lib_Xamarin.AndroidX.Navigation.Common.dll.so => 245
	i32 u0xbd78b0c8, ; 717: Xamarin.AndroidX.Navigation.Fragment.dll => 246
	i32 u0xbddce8a2, ; 718: lib_System.Security.Principal.dll.so => 129
	i32 u0xbe3f07c2, ; 719: lib_System.Runtime.CompilerServices.Unsafe.dll.so => 102
	i32 u0xbe4755f4, ; 720: System.Security.SecureString => 130
	i32 u0xbe592c0c, ; 721: System.Web => 154
	i32 u0xbefef58f, ; 722: System.Data.dll => 24
	i32 u0xbf506931, ; 723: System.Xml.XmlDocument.dll => 162
	i32 u0xbff2e236, ; 724: System.Threading => 149
	i32 u0xc04c3c0a, ; 725: System.Runtime.Handles.dll => 105
	i32 u0xc095e070, ; 726: lib_Xamarin.AndroidX.Lifecycle.Common.dll.so => 230
	i32 u0xc10b79a7, ; 727: Xamarin.AndroidX.Core.ViewTree => 218
	i32 u0xc1c6ebf4, ; 728: System.Reflection.DispatchProxy.dll => 90
	i32 u0xc217efb6, ; 729: lib_Xamarin.AndroidX.ConstraintLayout.Core.dll.so => 214
	i32 u0xc235e84d, ; 730: Xamarin.AndroidX.CardView => 208
	i32 u0xc2a37b91, ; 731: System.Linq.Queryable.dll => 61
	i32 u0xc2a993fa, ; 732: System.Threading.Tasks.Extensions.dll => 143
	i32 u0xc3428433, ; 733: lib_System.Reflection.Emit.ILGeneration.dll.so => 91
	i32 u0xc35f7fa4, ; 734: System.Resources.Writer.dll => 101
	i32 u0xc37f65ce, ; 735: Microsoft.Win32.Registry.dll => 5
	i32 u0xc3888e16, ; 736: System.ComponentModel.Annotations.dll => 13
	i32 u0xc3ba1d80, ; 737: lib_System.Security.Cryptography.Csp.dll.so => 122
	i32 u0xc3e9b3a2, ; 738: SQLite-net.dll => 189
	i32 u0xc4251ff9, ; 739: System.Security.Cryptography.Encoding => 123
	i32 u0xc4684d9e, ; 740: lib_System.Security.Cryptography.Algorithms.dll.so => 120
	i32 u0xc4a8494a, ; 741: System.Text.Encoding => 136
	i32 u0xc4e76306, ; 742: System.Diagnostics.FileVersionInfo => 28
	i32 u0xc52b3930, ; 743: StokBarangMAUI => 0
	i32 u0xc591efe9, ; 744: lib_Microsoft.Extensions.Configuration.Abstractions.dll.so => 176
	i32 u0xc5b097e4, ; 745: System.Net.Requests.dll => 73
	i32 u0xc5b776df, ; 746: Xamarin.AndroidX.CustomView.dll => 220
	i32 u0xc5b79d28, ; 747: System.Data => 24
	i32 u0xc69f3b41, ; 748: lib_System.Data.dll.so => 24
	i32 u0xc71af05d, ; 749: Xamarin.AndroidX.Arch.Core.Runtime => 206
	i32 u0xc76e512c, ; 750: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller.dll => 249
	i32 u0xc774da4f, ; 751: Xamarin.AndroidX.Navigation.Runtime => 247
	i32 u0xc7a3b0f0, ; 752: lib_Xamarin.AndroidX.Transition.dll.so => 259
	i32 u0xc7b797d0, ; 753: lib_Xamarin.AndroidX.Core.Core.Ktx.dll.so => 217
	i32 u0xc821fc10, ; 754: lib_System.ComponentModel.dll.so => 18
	i32 u0xc82afec1, ; 755: System.Text.Json => 138
	i32 u0xc849ca45, ; 756: SQLitePCLRaw.core => 191
	i32 u0xc8693088, ; 757: Xamarin.AndroidX.Activity.Ktx => 199
	i32 u0xc86c06e3, ; 758: Xamarin.AndroidX.Core => 216
	i32 u0xc8a662e9, ; 759: Java.Interop => 169
	i32 u0xc8d10307, ; 760: lib_System.Diagnostics.TraceSource.dll.so => 33
	i32 u0xc92a6809, ; 761: Xamarin.AndroidX.RecyclerView => 250
	i32 u0xca5de1fa, ; 762: System.Runtime.CompilerServices.Unsafe.dll => 102
	i32 u0xcae37e41, ; 763: System.Security.Cryptography.OpenSsl.dll => 124
	i32 u0xcaf7bd4b, ; 764: Xamarin.AndroidX.CustomView.PoolingContainer => 221
	i32 u0xcb5af55c, ; 765: lib_System.Reflection.Emit.Lightweight.dll.so => 92
	i32 u0xcc5af6ee, ; 766: Microsoft.Extensions.DependencyInjection.dll => 177
	i32 u0xcc6479a0, ; 767: System.Xml => 164
	i32 u0xcc7d82b4, ; 768: netstandard => 168
	i32 u0xcd1dd0db, ; 769: Xamarin.AndroidX.DynamicAnimation.dll => 223
	i32 u0xcd5a809f, ; 770: System.Formats.Tar => 39
	i32 u0xcdd8cd54, ; 771: lib_Xamarin.AndroidX.Emoji2.dll.so => 224
	i32 u0xce3fa116, ; 772: lib_System.Diagnostics.Process.dll.so => 29
	i32 u0xce70fda2, ; 773: hr/Microsoft.Maui.Controls.resources.dll => 294
	i32 u0xcef19b37, ; 774: System.ComponentModel.TypeConverter.dll => 17
	i32 u0xcf3163e6, ; 775: Mono.Android => 172
	i32 u0xcf663a21, ; 776: ru/Microsoft.Maui.Controls.resources.dll => 307
	i32 u0xcfa20c36, ; 777: lib_Xamarin.AndroidX.SwipeRefreshLayout.dll.so => 257
	i32 u0xcfbaacae, ; 778: System.Text.Json.dll => 138
	i32 u0xcfd0c798, ; 779: System.Transactions => 151
	i32 u0xd0418592, ; 780: Xamarin.AndroidX.Concurrent.Futures.dll => 212
	i32 u0xd0f73226, ; 781: lib_Xamarin.KotlinX.AtomicFU.Jvm.dll.so => 277
	i32 u0xd128d608, ; 782: System.Xml.Linq => 156
	i32 u0xd1854eb4, ; 783: System.Security.dll => 131
	i32 u0xd2757232, ; 784: System.Configuration => 19
	i32 u0xd2ff69f1, ; 785: System.Net.HttpListener => 66
	i32 u0xd310c033, ; 786: lib_Xamarin.Jetbrains.Annotations.dll.so => 273
	i32 u0xd328ac54, ; 787: vi/Microsoft.Maui.Controls.resources => 313
	i32 u0xd4045e1b, ; 788: lib_System.dll.so => 165
	i32 u0xd404ddfe, ; 789: lib_System.Runtime.Intrinsics.dll.so => 109
	i32 u0xd432d20b, ; 790: System.Threading.Timer => 148
	i32 u0xd457e5c9, ; 791: lib_Microsoft.CSharp.dll.so => 1
	i32 u0xd47cb45a, ; 792: lib_Xamarin.AndroidX.Arch.Core.Common.dll.so => 205
	i32 u0xd496c3c3, ; 793: lib_Xamarin.AndroidX.ExifInterface.dll.so => 226
	i32 u0xd4d2575b, ; 794: System.IO.FileSystem.AccessControl => 47
	i32 u0xd505225a, ; 795: lib_System.Xml.XPath.dll.so => 161
	i32 u0xd622b752, ; 796: lib-ro-Microsoft.Maui.Controls.resources.dll.so => 306
	i32 u0xd664cdf2, ; 797: de/Microsoft.Maui.Controls.resources => 287
	i32 u0xd6665034, ; 798: Xamarin.Android.Glide.GifDecoder.dll => 197
	i32 u0xd67a52b3, ; 799: System.Net.WebSockets.Client => 80
	i32 u0xd715a361, ; 800: System.Linq.dll => 62
	i32 u0xd7f95f5a, ; 801: da/Microsoft.Maui.Controls.resources => 286
	i32 u0xd804d57a, ; 802: System.Runtime.InteropServices.RuntimeInformation => 107
	i32 u0xd889aee8, ; 803: lib_System.Threading.Channels.dll.so => 140
	i32 u0xd8950487, ; 804: Xamarin.AndroidX.Annotation.Experimental => 201
	i32 u0xd8bba49d, ; 805: lib_Xamarin.AndroidX.RecyclerView.dll.so => 250
	i32 u0xd8dbab5d, ; 806: System.IO.FileSystem.Primitives.dll => 49
	i32 u0xd90e5f5a, ; 807: Xamarin.AndroidX.Lifecycle.LiveData.Core => 233
	i32 u0xd92e86f1, ; 808: Xamarin.KotlinX.Serialization.Core.dll => 281
	i32 u0xd930cda0, ; 809: Xamarin.AndroidX.Navigation.Fragment => 246
	i32 u0xd943a729, ; 810: System.ComponentModel.DataAnnotations => 14
	i32 u0xd96cf6f7, ; 811: pt-BR/Microsoft.Maui.Controls.resources => 304
	i32 u0xd9f65f5e, ; 812: lib-el-Microsoft.Maui.Controls.resources.dll.so => 288
	i32 u0xd9fdda56, ; 813: Microsoft.Extensions.Configuration.Abstractions.dll => 176
	i32 u0xda2f27df, ; 814: System.Net.NetworkInformation => 69
	i32 u0xda4773dd, ; 815: he/Microsoft.Maui.Controls.resources => 292
	i32 u0xdabf74ac, ; 816: lib_Xamarin.AndroidX.Annotation.Jvm.dll.so => 202
	i32 u0xdae8aa5e, ; 817: Mono.Android.dll => 172
	i32 u0xdb7f7e5d, ; 818: Xamarin.AndroidX.Browser => 207
	i32 u0xdb9df1ce, ; 819: Xamarin.AndroidX.Concurrent.Futures => 212
	i32 u0xdbb50d93, ; 820: ms/Microsoft.Maui.Controls.resources => 300
	i32 u0xdc5370c5, ; 821: lib_System.Web.HttpUtility.dll.so => 153
	i32 u0xdc68940c, ; 822: zh-Hant/Microsoft.Maui.Controls.resources.dll => 316
	i32 u0xdc96bdf5, ; 823: System.Net.WebProxy.dll => 79
	i32 u0xdcefb51d, ; 824: Xamarin.AndroidX.Core.Core.Ktx.dll => 217
	i32 u0xdd864306, ; 825: System.Runtime.Intrinsics => 109
	i32 u0xdda814c6, ; 826: Xamarin.AndroidX.Annotation => 200
	i32 u0xde068c70, ; 827: Xamarin.AndroidX.Navigation.Common.dll => 245
	i32 u0xde7354ab, ; 828: System.Net.NameResolution => 68
	i32 u0xdecad304, ; 829: System.Net.Http.Json.dll => 64
	i32 u0xdf1b1ecd, ; 830: lib_System.ServiceProcess.dll.so => 133
	i32 u0xdf6f3870, ; 831: System.Diagnostics.DiagnosticSource => 27
	i32 u0xdf9a7f42, ; 832: System.Xml.XPath => 161
	i32 u0xdfca27bc, ; 833: SQLitePCLRaw.provider.e_sqlite3 => 193
	i32 u0xdfd65a5d, ; 834: lib_System.Diagnostics.Tracing.dll.so => 34
	i32 u0xe05b6245, ; 835: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android.dll => 239
	i32 u0xe12f62fc, ; 836: lib_System.Threading.ThreadPool.dll.so => 147
	i32 u0xe13414bb, ; 837: lib-hu-Microsoft.Maui.Controls.resources.dll.so => 295
	i32 u0xe1a41194, ; 838: lib_System.Xml.XDocument.dll.so => 159
	i32 u0xe1ae15d6, ; 839: Xamarin.AndroidX.Collection => 209
	i32 u0xe1eea3e4, ; 840: lib_System.IO.Compression.ZipFile.dll.so => 45
	i32 u0xe1f0a5d8, ; 841: lib_Xamarin.AndroidX.ViewPager.dll.so => 263
	i32 u0xe2098b0b, ; 842: System.Collections.NonGeneric => 10
	i32 u0xe250cda6, ; 843: lib_Microsoft.Extensions.Logging.dll.so => 179
	i32 u0xe2513246, ; 844: lib_System.Runtime.Numerics.dll.so => 111
	i32 u0xe27c1b41, ; 845: lib_Xamarin.KotlinX.AtomicFU.dll.so => 276
	i32 u0xe2a3f2e8, ; 846: System.Collections.Specialized.dll => 11
	i32 u0xe34ee011, ; 847: lib_System.IO.Pipelines.dll.so => 54
	i32 u0xe3774f52, ; 848: lib_System.IO.MemoryMappedFiles.dll.so => 53
	i32 u0xe37fb0d2, ; 849: lib_StokBarangMAUI.dll.so => 0
	i32 u0xe3a54a09, ; 850: System.Net.WebProxy => 79
	i32 u0xe3c7860c, ; 851: lib_System.Security.Claims.dll.so => 119
	i32 u0xe3df9d2b, ; 852: System.Security.Cryptography.dll => 127
	i32 u0xe4436460, ; 853: System.Numerics.dll => 84
	i32 u0xe4fab729, ; 854: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 178
	i32 u0xe52378b9, ; 855: System.Net.Mail.dll => 67
	i32 u0xe56ef253, ; 856: System.Runtime.InteropServices.dll => 108
	i32 u0xe625b819, ; 857: lib_Xamarin.AndroidX.CardView.dll.so => 208
	i32 u0xe6b14171, ; 858: System.Net.HttpListener.dll => 66
	i32 u0xe6ca3640, ; 859: lib_Xamarin.AndroidX.Collection.Ktx.dll.so => 211
	i32 u0xe6e179fa, ; 860: System.Security.Principal => 129
	i32 u0xe6f98713, ; 861: System.Security.Cryptography.Encoding.dll => 123
	i32 u0xe70c9739, ; 862: SQLite-net => 189
	i32 u0xe797fcc1, ; 863: System.Net.WebHeaderCollection.dll => 78
	i32 u0xe79e77a6, ; 864: Xamarin.AndroidX.Transition.dll => 259
	i32 u0xe7c9e2bd, ; 865: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller => 249
	i32 u0xe7dc15ff, ; 866: zh-Hans/Microsoft.Maui.Controls.resources.dll => 315
	i32 u0xe839deed, ; 867: System.Collections.Concurrent.dll => 8
	i32 u0xe843daa0, ; 868: Xamarin.AndroidX.Core.dll => 216
	i32 u0xe89260c1, ; 869: Microsoft.VisualBasic.Core.dll => 2
	i32 u0xe90fdb70, ; 870: Xamarin.AndroidX.Collection.Jvm => 210
	i32 u0xe92ace5f, ; 871: lib_System.Linq.Parallel.dll.so => 60
	i32 u0xe99f7d24, ; 872: lib-tr-Microsoft.Maui.Controls.resources.dll.so => 311
	i32 u0xe9b2d35e, ; 873: System.IO.Compression.FileSystem.dll => 44
	i32 u0xe9b630ed, ; 874: Xamarin.AndroidX.VersionedParcelable.dll => 262
	i32 u0xea0092d6, ; 875: lib_System.Threading.Tasks.Extensions.dll.so => 143
	i32 u0xea213423, ; 876: System.Xml.ReaderWriter => 157
	i32 u0xea4780ec, ; 877: System.Security.Principal.dll => 129
	i32 u0xea4fb52e, ; 878: Xamarin.AndroidX.Navigation.UI => 248
	i32 u0xeab81858, ; 879: lib_Microsoft.Maui.Essentials.dll.so => 187
	i32 u0xeaf244cc, ; 880: lib_System.IO.Pipes.AccessControl.dll.so => 55
	i32 u0xeaf598f6, ; 881: lib_Microsoft.Extensions.Logging.Abstractions.dll.so => 180
	i32 u0xeb2ecede, ; 882: System.Data.DataSetExtensions.dll => 23
	i32 u0xeb5560c9, ; 883: lib_System.Runtime.InteropServices.RuntimeInformation.dll.so => 107
	i32 u0xebac8bfe, ; 884: System.Text.Encoding.CodePages => 134
	i32 u0xebb0254b, ; 885: lib_System.Net.NetworkInformation.dll.so => 69
	i32 u0xebc66336, ; 886: Xamarin.AndroidX.AppCompat.dll => 203
	i32 u0xec05582d, ; 887: Xamarin.AndroidX.Lifecycle.Process => 235
	i32 u0xed1090ae, ; 888: lib_System.Net.Primitives.dll.so => 71
	i32 u0xed409aea, ; 889: th/Microsoft.Maui.Controls.resources.dll => 310
	i32 u0xed96d41f, ; 890: lib_Xamarin.AndroidX.CoordinatorLayout.dll.so => 215
	i32 u0xedadd6e2, ; 891: he/Microsoft.Maui.Controls.resources.dll => 292
	i32 u0xedf6669b, ; 892: lib_System.Drawing.dll.so => 36
	i32 u0xee9f991d, ; 893: System.Diagnostics.Process.dll => 29
	i32 u0xeeefb9c8, ; 894: lib_System.Dynamic.Runtime.dll.so => 37
	i32 u0xef5e8475, ; 895: Xamarin.AndroidX.Annotation.Jvm.dll => 202
	i32 u0xefd01a89, ; 896: System.IO.Pipelines => 54
	i32 u0xeff49a63, ; 897: System.Memory => 63
	i32 u0xeff49c4a, ; 898: lib_System.Text.Encoding.Extensions.dll.so => 135
	i32 u0xf09122fc, ; 899: lib_System.IO.IsolatedStorage.dll.so => 52
	i32 u0xf121f953, ; 900: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.dll.so => 233
	i32 u0xf1304331, ; 901: Microsoft.Maui.Controls.Xaml.dll => 185
	i32 u0xf15cb56d, ; 902: Xamarin.KotlinX.Serialization.Core => 281
	i32 u0xf1676aaa, ; 903: lib-da-Microsoft.Maui.Controls.resources.dll.so => 286
	i32 u0xf1ad867b, ; 904: System.Reflection.Emit.ILGeneration => 91
	i32 u0xf27f60d1, ; 905: System.Private.Xml.Linq.dll => 88
	i32 u0xf29c5384, ; 906: id/Microsoft.Maui.Controls.resources => 296
	i32 u0xf2ce3c98, ; 907: System.Threading.dll => 149
	i32 u0xf2dd3fc4, ; 908: lib-ja-Microsoft.Maui.Controls.resources.dll.so => 298
	i32 u0xf323e0a6, ; 909: lib_Xamarin.Kotlin.StdLib.dll.so => 275
	i32 u0xf33c42ef, ; 910: lib_Xamarin.AndroidX.VectorDrawable.Animated.dll.so => 261
	i32 u0xf3a16066, ; 911: lib_Xamarin.AndroidX.Lifecycle.ViewModel.dll.so => 240
	i32 u0xf40add04, ; 912: Microsoft.Maui.Essentials.dll => 187
	i32 u0xf42589bc, ; 913: lib_System.Security.Cryptography.X509Certificates.dll.so => 126
	i32 u0xf45985cf, ; 914: System.Drawing.dll => 36
	i32 u0xf462c30d, ; 915: System.Private.Uri => 87
	i32 u0xf479582c, ; 916: Xamarin.AndroidX.Emoji2 => 224
	i32 u0xf47b0a29, ; 917: lib_System.Configuration.dll.so => 19
	i32 u0xf48143e5, ; 918: pt/Microsoft.Maui.Controls.resources.dll => 305
	i32 u0xf5185c24, ; 919: lib-pt-Microsoft.Maui.Controls.resources.dll.so => 305
	i32 u0xf53cb11d, ; 920: lib_System.Net.Security.dll.so => 74
	i32 u0xf5861a4f, ; 921: pl/Microsoft.Maui.Controls.resources => 303
	i32 u0xf5e94e90, ; 922: ms/Microsoft.Maui.Controls.resources.dll => 300
	i32 u0xf5f4f1f0, ; 923: Microsoft.Extensions.DependencyInjection => 177
	i32 u0xf5fdf056, ; 924: lib_Microsoft.Extensions.DependencyInjection.dll.so => 177
	i32 u0xf60736e2, ; 925: System.IO.FileSystem.Watcher => 50
	i32 u0xf6318da0, ; 926: System.AppContext => 6
	i32 u0xf73be021, ; 927: System.Reflection.Emit.ILGeneration.dll => 91
	i32 u0xf76edc75, ; 928: System.Core => 21
	i32 u0xf7e95c85, ; 929: System.Xml.XmlSerializer => 163
	i32 u0xf807b767, ; 930: System.Reflection.TypeExtensions => 97
	i32 u0xf83dd773, ; 931: System.IO.FileSystem.Watcher.dll => 50
	i32 u0xf86129d4, ; 932: lib-sv-Microsoft.Maui.Controls.resources.dll.so => 309
	i32 u0xf93ba7d4, ; 933: System.Runtime.Serialization.Primitives => 114
	i32 u0xf94a8f86, ; 934: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 243
	i32 u0xf97c5a99, ; 935: System.Security => 131
	i32 u0xf9be026d, ; 936: lib_SQLitePCLRaw.core.dll.so => 191
	i32 u0xfa21f6af, ; 937: System.Net.WebClient.dll => 77
	i32 u0xfa50891f, ; 938: lib_System.Linq.dll.so => 62
	i32 u0xfa6ae1e2, ; 939: lib_Xamarin.AndroidX.Annotation.dll.so => 200
	i32 u0xfb0af295, ; 940: lib-zh-HK-Microsoft.Maui.Controls.resources.dll.so => 314
	i32 u0xfb1dad5d, ; 941: System.Diagnostics.DiagnosticSource.dll => 27
	i32 u0xfbc4b67c, ; 942: lib_System.IO.Compression.Brotli.dll.so => 43
	i32 u0xfc0a7526, ; 943: Xamarin.KotlinX.AtomicFU.Jvm.dll => 277
	i32 u0xfc5f7d36, ; 944: pt/Microsoft.Maui.Controls.resources => 305
	i32 u0xfdaee526, ; 945: Xamarin.AndroidX.Core.Core.Ktx => 217
	i32 u0xfdd1b433, ; 946: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx => 242
	i32 u0xfdf2741f, ; 947: System.Buffers => 7
	i32 u0xfe42d509, ; 948: lib_Xamarin.AndroidX.Security.SecurityCrypto.dll.so => 254
	i32 u0xfea12dee, ; 949: Microsoft.Maui.Controls.dll => 184
	i32 u0xfecef6ea, ; 950: System.Runtime.Numerics => 111
	i32 u0xff912ee3, ; 951: lib_System.Xml.Serialization.dll.so => 158
	i32 u0xffd4917f, ; 952: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 243
	i32 u0xfffce3e8 ; 953: Xamarin.AndroidX.ExifInterface.dll => 226
], align 4

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [954 x i32] [
	i32 69, i32 73, i32 68, i32 244, i32 163, i32 109, i32 178, i32 236,
	i32 272, i32 48, i32 81, i32 312, i32 146, i32 277, i32 186, i32 284,
	i32 30, i32 125, i32 188, i32 103, i32 10, i32 255, i32 61, i32 314,
	i32 272, i32 67, i32 108, i32 255, i32 140, i32 31, i32 78, i32 125,
	i32 13, i32 212, i32 209, i32 276, i32 198, i32 133, i32 257, i32 258,
	i32 313, i32 267, i32 152, i32 160, i32 64, i32 303, i32 313, i32 75,
	i32 314, i32 18, i32 207, i32 44, i32 26, i32 181, i32 1, i32 218,
	i32 220, i32 60, i32 42, i32 301, i32 92, i32 130, i32 194, i32 213,
	i32 148, i32 232, i32 229, i32 285, i32 23, i32 308, i32 55, i32 70,
	i32 198, i32 84, i32 269, i32 117, i32 298, i32 230, i32 192, i32 13,
	i32 297, i32 285, i32 132, i32 175, i32 110, i32 56, i32 150, i32 75,
	i32 146, i32 63, i32 147, i32 251, i32 317, i32 166, i32 237, i32 123,
	i32 309, i32 214, i32 12, i32 227, i32 126, i32 153, i32 238, i32 171,
	i32 114, i32 167, i32 169, i32 165, i32 229, i32 87, i32 255, i32 116,
	i32 85, i32 296, i32 290, i32 183, i32 125, i32 6, i32 151, i32 293,
	i32 61, i32 179, i32 51, i32 104, i32 115, i32 40, i32 274, i32 268,
	i32 218, i32 266, i32 121, i32 304, i32 174, i32 132, i32 11, i32 52,
	i32 21, i32 44, i32 76, i32 120, i32 237, i32 220, i32 225, i32 221,
	i32 94, i32 287, i32 82, i32 137, i32 262, i32 22, i32 205, i32 8,
	i32 51, i32 304, i32 276, i32 74, i32 284, i32 156, i32 278, i32 219,
	i32 155, i32 164, i32 93, i32 273, i32 45, i32 299, i32 287, i32 271,
	i32 110, i32 131, i32 130, i32 190, i32 25, i32 195, i32 162, i32 73,
	i32 56, i32 46, i32 98, i32 182, i32 90, i32 221, i32 22, i32 234,
	i32 146, i32 87, i32 43, i32 161, i32 72, i32 222, i32 283, i32 3,
	i32 42, i32 64, i32 56, i32 192, i32 270, i32 16, i32 58, i32 53,
	i32 151, i32 41, i32 311, i32 272, i32 14, i32 241, i32 225, i32 106,
	i32 231, i32 167, i32 269, i32 230, i32 34, i32 159, i32 278, i32 86,
	i32 32, i32 223, i32 12, i32 315, i32 142, i32 51, i32 289, i32 57,
	i32 252, i32 36, i32 178, i32 286, i32 270, i32 203, i32 35, i32 229,
	i32 59, i32 97, i32 240, i32 12, i32 99, i32 183, i32 235, i32 174,
	i32 17, i32 275, i32 165, i32 93, i32 137, i32 197, i32 238, i32 181,
	i32 310, i32 124, i32 17, i32 79, i32 83, i32 265, i32 65, i32 86,
	i32 155, i32 154, i32 261, i32 247, i32 290, i32 141, i32 101, i32 303,
	i32 205, i32 29, i32 52, i32 209, i32 148, i32 301, i32 293, i32 266,
	i32 210, i32 274, i32 5, i32 188, i32 282, i32 256, i32 279, i32 88,
	i32 260, i32 115, i32 211, i32 278, i32 202, i32 191, i32 222, i32 249,
	i32 86, i32 266, i32 265, i32 62, i32 113, i32 294, i32 284, i32 58,
	i32 252, i32 100, i32 173, i32 50, i32 19, i32 215, i32 112, i32 102,
	i32 32, i32 3, i32 103, i32 84, i32 105, i32 269, i32 231, i32 72,
	i32 4, i32 241, i32 38, i32 32, i32 57, i32 104, i32 74, i32 9,
	i32 124, i32 46, i32 204, i32 183, i32 0, i32 9, i32 43, i32 4,
	i32 253, i32 293, i32 35, i32 288, i32 31, i32 207, i32 139, i32 93,
	i32 94, i32 308, i32 291, i32 49, i32 142, i32 113, i32 282, i32 141,
	i32 2, i32 309, i32 16, i32 316, i32 116, i32 270, i32 158, i32 77,
	i32 80, i32 39, i32 244, i32 37, i32 264, i32 206, i32 225, i32 219,
	i32 65, i32 139, i32 15, i32 46, i32 157, i32 166, i32 117, i32 280,
	i32 258, i32 267, i32 213, i32 48, i32 71, i32 81, i32 274, i32 127,
	i32 95, i32 122, i32 150, i32 26, i32 192, i32 118, i32 234, i32 98,
	i32 28, i32 208, i32 103, i32 306, i32 150, i32 54, i32 170, i32 4,
	i32 99, i32 38, i32 316, i32 33, i32 78, i32 94, i32 257, i32 179,
	i32 106, i32 21, i32 41, i32 203, i32 171, i32 104, i32 227, i32 149,
	i32 291, i32 244, i32 301, i32 275, i32 267, i32 279, i32 248, i32 2,
	i32 295, i32 135, i32 112, i32 262, i32 180, i32 312, i32 195, i32 59,
	i32 96, i32 213, i32 39, i32 206, i32 315, i32 25, i32 95, i32 90,
	i32 100, i32 10, i32 204, i32 172, i32 193, i32 88, i32 49, i32 63,
	i32 77, i32 101, i32 298, i32 254, i32 175, i32 47, i32 197, i32 7,
	i32 294, i32 82, i32 240, i32 283, i32 201, i32 194, i32 292, i32 297,
	i32 89, i32 233, i32 155, i32 317, i32 228, i32 33, i32 113, i32 117,
	i32 66, i32 83, i32 271, i32 193, i32 136, i32 20, i32 11, i32 163,
	i32 134, i32 299, i32 3, i32 28, i32 299, i32 300, i32 268, i32 186,
	i32 112, i32 306, i32 100, i32 27, i32 15, i32 182, i32 7, i32 180,
	i32 85, i32 59, i32 72, i32 30, i32 280, i32 65, i32 261, i32 144,
	i32 81, i32 307, i32 158, i32 41, i32 242, i32 184, i32 118, i32 176,
	i32 196, i32 291, i32 297, i32 250, i32 302, i32 168, i32 310, i32 237,
	i32 132, i32 76, i32 190, i32 67, i32 173, i32 200, i32 144, i32 107,
	i32 152, i32 290, i32 20, i32 71, i32 189, i32 232, i32 157, i32 311,
	i32 175, i32 145, i32 122, i32 246, i32 283, i32 128, i32 153, i32 224,
	i32 154, i32 89, i32 142, i32 302, i32 211, i32 96, i32 227, i32 20,
	i32 14, i32 256, i32 136, i32 76, i32 60, i32 190, i32 214, i32 168,
	i32 234, i32 169, i32 184, i32 15, i32 75, i32 138, i32 236, i32 6,
	i32 296, i32 199, i32 23, i32 236, i32 252, i32 308, i32 194, i32 185,
	i32 92, i32 1, i32 137, i32 239, i32 238, i32 260, i32 135, i32 70,
	i32 147, i32 114, i32 280, i32 25, i32 228, i32 181, i32 89, i32 97,
	i32 219, i32 223, i32 248, i32 31, i32 45, i32 144, i32 232, i32 243,
	i32 196, i32 110, i32 159, i32 35, i32 279, i32 108, i32 22, i32 115,
	i32 58, i32 139, i32 258, i32 145, i32 119, i32 121, i32 216, i32 111,
	i32 289, i32 198, i32 140, i32 204, i32 55, i32 80, i32 106, i32 185,
	i32 8, i32 186, i32 121, i32 134, i32 241, i32 273, i32 265, i32 152,
	i32 263, i32 251, i32 231, i32 9, i32 195, i32 48, i32 228, i32 68,
	i32 271, i32 188, i32 312, i32 160, i32 289, i32 247, i32 215, i32 5,
	i32 164, i32 127, i32 133, i32 251, i32 162, i32 302, i32 242, i32 141,
	i32 263, i32 42, i32 259, i32 288, i32 170, i32 187, i32 199, i32 268,
	i32 40, i32 282, i32 307, i32 226, i32 82, i32 57, i32 37, i32 128,
	i32 98, i32 167, i32 173, i32 281, i32 264, i32 83, i32 201, i32 182,
	i32 99, i32 26, i32 30, i32 160, i32 253, i32 18, i32 128, i32 120,
	i32 222, i32 156, i32 239, i32 254, i32 285, i32 196, i32 70, i32 235,
	i32 256, i32 166, i32 317, i32 105, i32 253, i32 245, i32 170, i32 171,
	i32 16, i32 264, i32 260, i32 145, i32 295, i32 40, i32 126, i32 119,
	i32 38, i32 116, i32 47, i32 143, i32 85, i32 118, i32 34, i32 174,
	i32 96, i32 53, i32 210, i32 95, i32 245, i32 246, i32 129, i32 102,
	i32 130, i32 154, i32 24, i32 162, i32 149, i32 105, i32 230, i32 218,
	i32 90, i32 214, i32 208, i32 61, i32 143, i32 91, i32 101, i32 5,
	i32 13, i32 122, i32 189, i32 123, i32 120, i32 136, i32 28, i32 0,
	i32 176, i32 73, i32 220, i32 24, i32 24, i32 206, i32 249, i32 247,
	i32 259, i32 217, i32 18, i32 138, i32 191, i32 199, i32 216, i32 169,
	i32 33, i32 250, i32 102, i32 124, i32 221, i32 92, i32 177, i32 164,
	i32 168, i32 223, i32 39, i32 224, i32 29, i32 294, i32 17, i32 172,
	i32 307, i32 257, i32 138, i32 151, i32 212, i32 277, i32 156, i32 131,
	i32 19, i32 66, i32 273, i32 313, i32 165, i32 109, i32 148, i32 1,
	i32 205, i32 226, i32 47, i32 161, i32 306, i32 287, i32 197, i32 80,
	i32 62, i32 286, i32 107, i32 140, i32 201, i32 250, i32 49, i32 233,
	i32 281, i32 246, i32 14, i32 304, i32 288, i32 176, i32 69, i32 292,
	i32 202, i32 172, i32 207, i32 212, i32 300, i32 153, i32 316, i32 79,
	i32 217, i32 109, i32 200, i32 245, i32 68, i32 64, i32 133, i32 27,
	i32 161, i32 193, i32 34, i32 239, i32 147, i32 295, i32 159, i32 209,
	i32 45, i32 263, i32 10, i32 179, i32 111, i32 276, i32 11, i32 54,
	i32 53, i32 0, i32 79, i32 119, i32 127, i32 84, i32 178, i32 67,
	i32 108, i32 208, i32 66, i32 211, i32 129, i32 123, i32 189, i32 78,
	i32 259, i32 249, i32 315, i32 8, i32 216, i32 2, i32 210, i32 60,
	i32 311, i32 44, i32 262, i32 143, i32 157, i32 129, i32 248, i32 187,
	i32 55, i32 180, i32 23, i32 107, i32 134, i32 69, i32 203, i32 235,
	i32 71, i32 310, i32 215, i32 292, i32 36, i32 29, i32 37, i32 202,
	i32 54, i32 63, i32 135, i32 52, i32 233, i32 185, i32 281, i32 286,
	i32 91, i32 88, i32 296, i32 149, i32 298, i32 275, i32 261, i32 240,
	i32 187, i32 126, i32 36, i32 87, i32 224, i32 19, i32 305, i32 305,
	i32 74, i32 303, i32 300, i32 177, i32 177, i32 50, i32 6, i32 91,
	i32 21, i32 163, i32 97, i32 50, i32 309, i32 114, i32 243, i32 131,
	i32 191, i32 77, i32 62, i32 200, i32 314, i32 27, i32 43, i32 277,
	i32 305, i32 217, i32 242, i32 7, i32 254, i32 184, i32 111, i32 158,
	i32 243, i32 226
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
attributes #0 = { memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress "no-trapping-math"="true" nofree norecurse nosync nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+armv7-a,+d32,+dsp,+fp64,+neon,+vfp2,+vfp2sp,+vfp3,+vfp3d16,+vfp3d16sp,+vfp3sp,-aes,-fp-armv8,-fp-armv8d16,-fp-armv8d16sp,-fp-armv8sp,-fp16,-fp16fml,-fullfp16,-sha2,-thumb-mode,-vfp4,-vfp4d16,-vfp4d16sp,-vfp4sp" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { "no-trapping-math"="true" noreturn nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+armv7-a,+d32,+dsp,+fp64,+neon,+vfp2,+vfp2sp,+vfp3,+vfp3d16,+vfp3d16sp,+vfp3sp,-aes,-fp-armv8,-fp-armv8d16,-fp-armv8d16sp,-fp-armv8sp,-fp16,-fp16fml,-fullfp16,-sha2,-thumb-mode,-vfp4,-vfp4d16,-vfp4d16sp,-vfp4sp" }

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
