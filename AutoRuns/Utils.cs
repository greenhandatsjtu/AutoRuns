using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32;

namespace AutoRuns
{
    public class Utils
    {
        /// <summary>
        ///     根据ImagePath获取文件路径
        /// </summary>
        /// <param name="imagePath">ImagePath键值</param>
        /// <returns>文件真实路径</returns>
        public static string GetFilePath(string imagePath)
        {
            //有些程序路径包含在双引号中，只要读出双引号包含的路径即可
            if (imagePath.Contains("\""))
            {
                imagePath = imagePath.Split("\"")[1];
            }
            // 还有些ImagePath包括参数，需要删除后面的参数，只保留真实路径
            else if (imagePath.Contains(" "))
            {
                var elements = imagePath.Split(" ");
                for (var i = 0; i < elements.Length; i++)
                    //读到参数的地方就停下，将其前面的字符串存入真实路径的变量path
                    if (elements[i].StartsWith("-") || elements[i].StartsWith("/"))
                    {
                        imagePath = string.Join(" ", elements.Take(i));
                        break;
                    }
            }

            return imagePath;
        }

        /// <summary>
        ///     获取描述和发布者等信息
        /// </summary>
        /// <param name="path">程序路径</param>
        /// <returns>FileVersionInfo</returns>
        public static FileVersionInfo FetchInfo(string path)
        {
            path = GetFilePath(path);

            try
            {
                return FileVersionInfo.GetVersionInfo(path);
            }
            catch
            {
                return null;
            }
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegLoadMUIString(
            IntPtr registryKeyHandle, string value,
            StringBuilder outputBuffer, int outputBufferSize, out int requiredSize,
            RegistryLoadMuiStringOptions options, string path);

        /// <summary>
        ///     Retrieves the multilingual string associated with the specified name. Returns null if the name/value pair does not
        ///     exist in the registry.
        ///     The key must have been opened using
        /// </summary>
        /// <param name="key">The registry key to load the string from.</param>
        /// <param name="name">The name of the string to load.</param>
        /// <returns>The language-specific string, or null if the name/value pair does not exist in the registry.</returns>
        public static string LoadMuiStringValue(RegistryKey key, string name)
        {
            const int initialBufferSize = 1024;
            var output = new StringBuilder(initialBufferSize);
            int requiredSize;
            var keyHandle = key.Handle.DangerousGetHandle();
            var result = (ErrorCode) RegLoadMUIString(keyHandle, name, output, output.Capacity, out requiredSize,
                RegistryLoadMuiStringOptions.None, null);

            if (result == ErrorCode.MoreData)
            {
                output.EnsureCapacity(requiredSize);
                result = (ErrorCode) RegLoadMUIString(keyHandle, name, output, output.Capacity, out requiredSize,
                    RegistryLoadMuiStringOptions.None, null);
            }

            return result == ErrorCode.Success ? output.ToString() : null;
        }

        /// <summary>
        ///     判断键值是否进行了重定向
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsRedirected(string value)
        {
            return value != null && value.StartsWith('@');
        }

        /// <summary>
        ///     Determines the behavior of <see cref="RegLoadMUIString" />.
        /// </summary>
        [Flags]
        internal enum RegistryLoadMuiStringOptions : uint
        {
            None = 0,

            /// <summary>
            ///     The string is truncated to fit the available size of the output buffer. If this flag is specified, copiedDataSize
            ///     must be NULL.
            /// </summary>
            Truncate = 1
        }

        // Snippet of ErrorCode.
        private enum ErrorCode
        {
            Success = 0x0000,
            MoreData = 0x00EA
        }

        /// <summary>
        /// 获取Publisher
        /// </summary>
        /// <param name="path">可执行文件路径</param>
        /// <returns>Publisher名字</returns>
        public static string GetPublisher(string path)
        {
            string result;
            var cert = X509Certificate.CreateFromSignedFile(path);
            var cert2 = new X509Certificate2(cert);
            result = cert2.GetNameInfo(X509NameType.SimpleName, false);
            // if (cert2.Verify())
            // {
            //     result = "(Verified)" + result;
            // }
            return result;
        }
        
        /// <summary>
        /// 过滤Microsoft
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool MS_filter(object item)
        {
            var record = item as Record;
            return string.IsNullOrEmpty(record.Publisher)||!record.Publisher.Contains("Microsoft Corporation");
        }
        
        /// <summary>
        /// 过滤Windows
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool win_filter(object item)
        {
            var record = item as Record;
            return string.IsNullOrEmpty(record.Publisher)||!record.Publisher.Contains("Microsoft Windows");
        }
        
        /// <summary>
        /// 同时过滤Microsoft和Windows
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool both_filter(object item)
        {
            var record = item as Record;
            return string.IsNullOrEmpty(record.Publisher)||!(record.Publisher.Contains("Microsoft Windows")||record.Publisher.Contains("Microsoft Corporation"));
        }
    }
}