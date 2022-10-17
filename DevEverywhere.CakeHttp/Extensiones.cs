//using System.Text.RegularExpressions;
//using System.Reflection;
//using System.Globalization;
//using System.Security.Cryptography;
//using System.Text;
//using System.Data;
//using System.Collections;
//using System.Xml;
//using System.Text.Json;
//using System.Net.Http.Headers;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Text.Json.Serialization;
//using System.Collections.Immutable;
//using System.Xml.Serialization;
//using System.Net;
//using System.Text.Encodings.Web;
//using System;

///// <summary>
///// Módulo de extensiones a objetos .NET, refactorizando algoritmos importantes a implementar
///// </summary>
//namespace WebHelpers
//{

//    internal static partial class Extensions
//    {        
//        public static JsonSerializerOptions DefaultRestifyJsonOptions { get; } = GetOptions();

//        readonly static Regex queryKeyParser = UrlParser();
//        readonly static Regex charsetParser = CharsetParser();

//        private static JsonSerializerOptions GetOptions()
//        {
//            var opts = new JsonSerializerOptions()
//            {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//#if DEBUG
//                WriteIndented = true
//#endif
//            };
//            return opts;
//        }



//        internal static bool CheckContinuation(ref this Utf8JsonReader json)
//        {
//            return json.Read() ? true : throw new InvalidExpressionException("Unexpected end when reading JSON.");
//        }

//        //public static async Task<TOut> GetAsync<TParams, TOut>(string path, TParams prms = default, Dictionary<string, string> headers = null)
//        //{
//        //    headers ??= new Dictionary<string, string>();
//        //    using HttpClient client = new HttpClient();
//        //    string uri = new UriBuilder(path).MergeParams(prms).Uri.ToString();
//        //    client.DefaultRequestHeaders.Add("Accept", "application/json");
//        //    foreach (var element in headers)
//        //        client.DefaultRequestHeaders.Add(element.Key, element.Value);
//        //    var utf8Json = await client.GetStreamAsync(uri);
//        //    return await JsonSerializer.DeserializeAsync<TOut>(utf8Json, DefaultJsonSerializerOptions);
//        //}

//        //        public static async Task<object> GetAsync(Type returnType, string path, object prms, Dictionary<string, string> headers = null)
//        //        {
//        //#if DEBUG 
//        //            var responseGuid = Guid.NewGuid();
//        //#endif
//        //            headers ??= new Dictionary<string, string>();
//        //            using HttpClient client = new HttpClient();
//        //            string uri = new UriBuilder(path).MergeParams(prms).Uri.ToString();
//        //            client.DefaultRequestHeaders.Add("Accept", "application/json");
//        //            foreach (var element in headers)
//        //                client.DefaultRequestHeaders.Add(element.Key, element.Value);
//        //#if DEBUG
//        //            Trace.WriteLine($@"{DateTime.Now.ToString("yyyy/MM/dd")}: ========> Request logs
//        //ID: {responseGuid}
//        //URL: {uri}
//        //");
//        //#endif
//        //            var utf8Json = await client.GetByteArrayAsync(uri);
//        //#if DEBUG
//        //            Trace.WriteLine($@"{DateTime.Now.ToString("yyyy/MM/dd")}: ========> Response logs
//        //{responseGuid}
//        //URL: {uri}
//        //Response: {JsonSerializer.Serialize(JsonDocument.Parse(utf8Json).RootElement, DefaultJsonSerializerOptions)}");
//        //#endif
//        //            return JsonSerializer.Deserialize(utf8Json, returnType, DefaultJsonSerializerOptions);
//        //        }

//        private static readonly object[] emptyParamteres = Array.Empty<object>();

//        public static async Task<object?> GetResponseAsync(Type returnType, HttpRequestMessage request, HttpClient client, Guid reqGuid = default, bool continueOnCapturedContext = false)
//        {
//#if DEBUG
//            reqGuid = reqGuid == default ? Guid.NewGuid() : Guid.NewGuid();
//#endif
//#if DEBUG
//            Trace.WriteLine($@"{DateTime.Now:yyyy/MM/dd}: ========> Request logs
//ID:             {reqGuid}
//Request:        {request}
//");
//#endif
//            try
//            {
//                using var response = await client.SendAsync(request).ConfigureAwait(continueOnCapturedContext);
//                if (response is { IsSuccessStatusCode: true, StatusCode: HttpStatusCode.OK, Content: { } content })
//                {
//                    var resultStream = await response.Content.ReadAsStreamAsync();

//                    var responseContent = await CreateResponse(returnType, response.Headers.GetValues("Content-Type").Join("; ").ToLower(), resultStream);
//#if DEBUG
//                    Trace.WriteLine($@"{DateTime.Now:yyyy/MM/dd}: ========> Response logs
//ID:             {reqGuid}
//Response:       {response}
//Data:           {(responseContent is object ret ? JsonSerializer.Serialize(ret, DefaultRestifyJsonOptions) : "(empty)")}");
//#endif
//                    return responseContent;
//                }
//            }catch 
//            {
//                throw;
//            }

//            return defaultGetterInfo.MakeGenericMethod(returnType).Invoke(null, emptyParamteres);
//        }

//        private static async Task<object?> CreateResponse(Type returnType, string _contentType, Stream resultStream)
//        {
//            if(resultStream.Length > 0)
//                if (_contentType.EndsWith("/json"))
//                    return await JsonSerializer.DeserializeAsync(resultStream, returnType, DefaultRestifyJsonOptions);
//                else if (_contentType.EndsWith("/xml") && returnType.IsSerializable)
//                    return new XmlSerializer(returnType).Deserialize(resultStream);
//                else if (_contentType.StartsWith("text/"))
//                {
//                    var encoder = GetEncoderFromContentTypeCharset(_contentType);
//                    using MemoryStream memStream = new();
//                    resultStream.CopyTo(memStream);
//                    return encoder.GetString(memStream.ToArray());
//                }
//            return null;
//        }

//        internal static Encoding GetEncoderFromContentTypeCharset(string? _contentType)
//        {
//            string charset = charsetParser.Match(_contentType ?? "").Groups["charset"] is { Value: { Length: > 0 } val } ? val : "8";
//#pragma warning disable SYSLIB0001 // El tipo o el miembro están obsoletos
//            return charset switch
//            {
//                "7" => Encoding.UTF7,
//                "16" => Encoding.Unicode,
//                "16b" => Encoding.BigEndianUnicode,
//                "32" => Encoding.UTF32,
//                "asci" => Encoding.ASCII,
//                _ => Encoding.UTF8
//            };
//#pragma warning restore SYSLIB0001 // El tipo o el miembro están obsoletos
//        }

//        public static Task<object?> GetAsync(Type returnType, string path, object prms, params (string, string)[] headers)
//        {
//#if DEBUG
//            var responseGuid = Guid.NewGuid();
//#endif
//            using HttpClient client = new ();
//            path = new UriBuilder(path).MergeParams(prms).Uri.ToString();
//            using HttpRequestMessage request = new (HttpMethod.Get, path);

//            if (!request.Headers.Contains("Accept"))
//                request.Headers.Add("Accept", "application/json");

//            foreach (var element in headers)
//                request.Headers.Add(element.Item1, element.Item2);

//            return GetResponseAsync(returnType, request, client
//#if DEBUG 
//,responseGuid
//#endif
//                );
//        }

//            internal static readonly MethodInfo 
//                defaultGetterInfo = typeof(Extensions).GetMethod(nameof(GetDefault), BindingFlags.Public | BindingFlags.Static)!,
//                tryCastGetterInfo = typeof(Extensions).GetMethod(nameof(TryCast), BindingFlags.Public | BindingFlags.Static)!,
//                propertiesGetterInfo = typeof(Extensions).GetMethod(nameof(GetPropertiesTuple), BindingFlags.Public | BindingFlags.Static)!;

//            public static object? GetDefaultFromType(this Type type)
//            {
//                return defaultGetterInfo.MakeGenericMethod(type).Invoke(null, emptyParamteres);
//            }
//            public static object? TryCastToType(this object value, Type type)
//            {
//                return tryCastGetterInfo.MakeGenericMethod(type).Invoke(null, new[] { value });
//            }

//        public static T GetDefault<T>()
//        {
//            return default!;
//        }

//        public static T TryCast<T>(this object value)
//        {
//            return value is T casted ? casted : default!;
//        }

//        public static Task<object?> PostAsync(Type returnType, string path, object prms, params (string, string)[] headers)
//        {
//#if DEBUG
//            var responseGuid = Guid.NewGuid();
//#endif
//            //headers ??= new Dictionary<string, string>();
//            Uri uri = new UriBuilder(path).Uri;
//            using HttpClient client = new ();
//            using HttpRequestMessage request = new (HttpMethod.Post, path);

//            request.Headers.Add("Accept", "application/json");

//            foreach (var element in headers)
//                request.Headers.Add(element.Item1, element.Item2);

//            request.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(prms, DefaultRestifyJsonOptions));
//            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

//            return GetResponseAsync(returnType, request, client);
//        }

//        //        public static async Task<object> PostAsync(Type returnType, string path, object prms, Dictionary<string, string> headers = null)
//        //        {
//        //#if DEBUG 
//        //            var responseGuid = Guid.NewGuid();
//        //#endif
//        //            headers ??= new Dictionary<string, string>();
//        //            Uri uri = new UriBuilder(path).Uri;
//        //            using HttpClient client = new HttpClient();
//        //            client.DefaultRequestHeaders.Add("Accept", "application/json");
//        //            foreach (var element in headers)
//        //                client.DefaultRequestHeaders.Add(element.Key, element.Value);
//        //            using ByteArrayContent body = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(prms, DefaultJsonSerializerOptions));
//        //            body.Headers.ContentType = new MediaTypeHeaderValue("application/json");
//        //#if DEBUG
//        //            Trace.WriteLine($@"{DateTime.Now.ToString("yyyy/MM/dd")}: ========> Request logs
//        //ID: {responseGuid}
//        //URL: {uri}
//        //Data: {JsonSerializer.Serialize(prms, DefaultJsonSerializerOptions)}
//        //");
//        //#endif
//        //            using HttpResponseMessage response = await client.PostAsync(uri, body);
//        //            var utf8Json = await response.Content.ReadAsByteArrayAsync();
//        //#if DEBUG
//        //            Trace.WriteLine($@"{DateTime.Now.ToString("yyyy/MM/dd")}: ========> Response logs
//        //{responseGuid}
//        //URL: {uri}
//        //Response: {JsonSerializer.Serialize(JsonDocument.Parse(utf8Json).RootElement, DefaultJsonSerializerOptions)}");
//        //#endif
//        //            object result = JsonSerializer.Deserialize(utf8Json, returnType, DefaultJsonSerializerOptions);
//        //            return result;
//        //        }

//        //public static async Task<TOut> PostAsync<TParams, TOut>(string path, TParams obj = default, Dictionary<string, string> headers = null)
//        //{
//        //    headers ??= new Dictionary<string, string>();
//        //    Uri uri = new UriBuilder(path).Uri;
//        //    using HttpClient client = new HttpClient();
//        //    client.DefaultRequestHeaders.Add("Accept", "application/json");
//        //    foreach (var element in headers)
//        //        client.DefaultRequestHeaders.Add(element.Key, element.Value);
//        //    using ByteArrayContent body = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(obj, DefaultJsonSerializerOptions));
//        //    body.Headers.ContentType = new MediaTypeHeaderValue("application/json");
//        //    using HttpResponseMessage response = await client.PostAsync(uri, body);
//        //    return JsonSerializer.Deserialize<TOut>(await response.Content.ReadAsByteArrayAsync(), DefaultJsonSerializerOptions);
//        //}

//        //public static async Task PostAsync<T>(this T local, string path) where T : class
//        //{
//        //    var result = await PostAsync<T>(path, local);

//        //    Merge(local, result);
//        //}

//        //public static async Task GetAsync<T>(this T local, string path, object prms = null) where T : class
//        //{
//        //    var result = await GetAsync<T>(path, prms);
//        //    Merge(local, result);
//        //}

//        //public static async Task GetAsync<T>(this InternalDbSet<T> local, string path, object prms = null) where T : class
//        //{
//        //    var result = await GetAsync<InternalDbSet<T>>(path, prms);
//        //    Merge(local, result);
//        //}

//        //private static void Merge(object obj, object r)
//        //{
//        //    var type = obj.GetType();
//        //    if (obj is DbContext localCtx && r is DbContext serverCtx)
//        //        MergeEntries(localCtx, serverCtx.ChangeTracker.Entries());
//        //    else if (type.Name.Contains("DbSet`1"))
//        //        MergeEntries(GetContext(obj), GetContext(r).ChangeTracker.Entries());
//        //    else if (obj is EntityEntry entry && entry.Context is DbContext entryCtx && r is EntityEntry serverEntry)
//        //        UpdateEntry(entryCtx, serverEntry);
//        //}

//        //private static void Merge<T>(InternalDbSet<T> obj, InternalDbSet<T> r) where T : class
//        //{
//        //    MergeEntries(GetContext(obj), GetContext(r).ChangeTracker.Entries<T>());
//        //}

//        //private static readonly FieldInfo dbSetContext = typeof(InternalDbSet<>).GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic);
//        ////private static readonly FieldInfo dbSetContext = typeof(InternalDbSet<>).GetMethod("_context", BindingFlags.Instance | BindingFlags.NonPublic);

//        //private static DbContext GetContext(object obj)
//        //{
//        //    return dbSetContext.GetValue(obj) as DbContext;
//        //}
//        //private static DbContext GetTypedEntries(ChangeTracker obj, Type entryType)
//        //{
//        //    return dbSetContext.GetValue(obj) as DbContext;
//        //}


//        //private static void MergeEntries(DbContext local, IEnumerable<EntityEntry> entries)
//        //{
//        //    foreach (var serverEntry in entries)
//        //        UpdateEntry(local, serverEntry);
//        //}

//        //private static void UpdateEntry(DbContext local, EntityEntry serverEntry)
//        //{
//        //    if (local.Entry(serverEntry.OriginalValues.ToObject()) is EntityEntry localEntry)
//        //        if (serverEntry.State == EntityState.Added || serverEntry.State == EntityState.Modified)
//        //        {
//        //            localEntry.CurrentValues.SetValues(serverEntry.CurrentValues);
//        //            localEntry.State = serverEntry.State;
//        //        }
//        //        else
//        //            local.Attach(serverEntry.Entity).State = serverEntry.State;

//        //}

//        public static void Apply<T>(this T receptor, T income) where T : INotifyPropertyChanged
//        {
//            if (receptor == null) return;
//            foreach (var prop in typeof(T).GetProperties())
//                if (prop.SetMethod is MethodInfo mi && prop.GetValue(income) is { } incomValueProp)
//                    if (prop.PropertyType == typeof(INotifyPropertyChanged) &&
//                        prop.GetValue(receptor) is INotifyPropertyChanged child && incomValueProp is INotifyPropertyChanged incomeChild)
//                        child.Apply(incomeChild);
//                    else
//                        mi.Invoke(receptor, new[] { incomValueProp });
//        }
//        public static string GetQuery<TValue>(List<(string key, TValue value)> props, bool useArrayIndexer = false)
//        {
//            return (from kv in props
//                    group kv by kv.key into g
//                    let last = g.Last()
//                    select (useArrayIndexer && last.value is IEnumerable<TValue> enumerable) ?
//                    (from v in enumerable
//                     select $"{UrlEncoder.Default.Encode(last.key)}[]=${v}").Join("&") :
//                    string.Format("{0}{1}", last.key, last.value == null ? "" : "=" + last.value)).Join("&");
//        }

//        public static UriBuilder MergeParams<T>(this UriBuilder bld, T props)
//        {
//            bld.Query = GetQuery((props is (string, object?)[] pdic ? pdic : GetPropertiesTuple<T, object?>(props, typeof(T))).ToList());
//            return bld;
//        }

//        public static (string, TOut?)[] GetPropertiesTuple<T, TOut>(T props, Type queryType, Func<string, string>? keyTransform = null, Func<object?, TOut>? valueTransform = null)
//        {
//            keyTransform ??= k => k;
//            valueTransform ??= o => o is { } _o ? (TOut)_o : default!;
//            var type = typeof(T);
//            Type objectType = typeof(object);
//            return GetTuples(props, keyTransform, valueTransform, type, objectType);
//        }

//        private static (string, TOut?)[] GetTuples<T, TOut>(T props, Func<string, string> keyTransform, Func<object?, TOut?> valueTransform, Type type, Type objectType)
//        {
//            return type == objectType && props != null
//                ? props.GetType().GetProperties().Select(p => (keyTransform(p.Name), valueTransform(p.GetValue(props, null)))).ToArray()
//                : props is { } val ? type.GetProperties().Select(p => (p.Name, valueTransform(p.GetValue(val, null)))).ToArray() :
//                    Array.Empty<(string, TOut?)>();
//        }

//        public static IEnumerable<KeyValuePair<string, TOut>> GetPropertiesDictionary<T, TOut>(T props, Func<string, string>? keyTransform = null, Func<object?, TOut>? valueTransform = null)
//        {
//            keyTransform ??= k => k;
//            valueTransform ??= o => o is { } _o ? (TOut)_o : default!;
//            var inputType = typeof(T);
//            return inputType == typeof(object) && props != null
//                ? props.GetType().GetProperties().ToDictionary(p => keyTransform(p.Name), p => valueTransform(p.GetValue(props, null)))
//                : props is T val ? inputType.GetProperties().ToDictionary(p => p.Name, p => valueTransform(p.GetValue(val, null))) :
//                    Array.Empty<KeyValuePair<string, TOut>>();
//        }

//        private static Dictionary<string, object?> GetQueryPartsInternal(UriBuilder bld)
//        {
//            Dictionary<string, object?> result = new();
//            string query = bld.Query;

//            foreach (Match match in queryKeyParser.Matches(query[(query.StartsWith("?") ? 1 : 0)..]))
//            {
//                if (match is { Groups: { } grps } && grps["key"] is { Value: { } key })
//                {
//                    var unescapedKey = Uri.UnescapeDataString(key);
//                    object? strValue = grps["hasValue"].Success 
//                        ? grps["value"] is { Success: true, Value: { } val } 
//                            ? Uri.UnescapeDataString(val) 
//                            : null 
//                        : true;

//                    if (result.TryGetValue(unescapedKey, out object? value) && value is IList<object?> list)
//                        list.Add(strValue);
//                    else if(grps["array"].Success || query[(match.Index + match.Length)..].Contains($"{key}="))
//                        result[unescapedKey] = new List<object?>() { strValue };
//                    else
//                        result[unescapedKey] = strValue;
//                }
//            }
//            return result;
//        }


//        public static bool HasChanges(this DataTable dataTable, DataRowState rowStates = DataRowState.Added | DataRowState.Deleted | DataRowState.Modified)
//        {
//            if ((rowStates & ~(DataRowState.Added | DataRowState.Deleted | DataRowState.Modified)) != 0)
//                throw new ArgumentOutOfRangeException(nameof(rowStates));

//            for (int j = 0; j < dataTable.Rows.Count; j++)
//                if ((dataTable.Rows[j].RowState & rowStates) != 0)
//                    return true;

//            return false;
//        }

//        static readonly MethodInfo rowTypeMethdInfo = typeof(DataTable).GetMethod("GetRowType", BindingFlags.Instance | BindingFlags.NonPublic)!;

//        public static Type? GetRowType(this DataTable dt)
//        {
//            return rowTypeMethdInfo.Invoke(dt, Array.Empty<object>()) as Type;
//        }

//        /// <summary>
//        /// Agrega una columna de datos a un objeto <see cref="DataTable"/>.
//        /// </summary>
//        /// <typeparam name="TColumn">Tipo de datos de la columna a agregar.</typeparam>
//        /// <param name="dt"><see cref="DataTable"/> destino de la adición.</param>
//        /// <param name="columnName">Nombre de la columna.</param>
//        /// <param name="identity">Determina si la columna es una autogenerada e incremental.</param>
//        /// <param name="seed">Establece el punto de inicio en caso de que el parámetro <paramref name="identity"/> = <see langword="True "/></param>
//        /// <param name="[step]">Paso a incrementar en caso de que el parámetro <paramref name="identity"/> = <see langword="True "/>.</param>
//        /// <param name="defValue">Valor por defecto de la columna</param>
//        /// <returns></returns>
//        public static DataColumn AddColumn<TColumn>(this DataTable dt, string columnName, bool identity = false, long seed = -1, long step = -1, TColumn defValue = default!)

//        {
//            Type originalTyp = typeof(TColumn);
//            Type? typ = Nullable.GetUnderlyingType(originalTyp);
//            DataColumn tempVar = dt.Columns.Add(columnName, typ ?? originalTyp);
//            if (identity)
//            {
//                tempVar.AutoIncrement = true;
//                tempVar.AutoIncrementSeed = seed;
//                tempVar.AutoIncrementSeed = step;
//            }
//            if (typ != null)
//            {
//                tempVar.AllowDBNull = true;
//            }
//            tempVar.DefaultValue = defValue ?? (object)DBNull.Value;
//            return tempVar;
//        }

//        public static string GetRecursiveExceptionText(Exception? exc)
//        {
//            string tempGetRecursiveExceptionText = "";
//            if (exc is { })
//            {
//                tempGetRecursiveExceptionText += GetRecursiveExceptionText(exc.InnerException) + newLine;
//                tempGetRecursiveExceptionText += GetExceptionMessage(exc) + newLine + exc.StackTrace + newLine;
//            }
//            return tempGetRecursiveExceptionText;
//        }

//        private static string GetExceptionMessage(Exception ex)
//        {
//            string tempGetExceptionMessage = ex.Message;
//            if (ex as ReflectionTypeLoadException is { LoaderExceptions: { } loaderExceptions }  )
//            {
//                tempGetExceptionMessage += "ReflectionTypeLoadException : " + loaderExceptions.Select((item) => item?.Message ?? "").Join(newLine);
//            }
//            return tempGetExceptionMessage;
//        }

//        private static readonly Type _int = typeof(int);
//        //private static readonly string folderHash = "SOFTWARE\\iAra\\";
//        private static readonly string[] umTamanno = { "b", "Kb", "Mb", "Gb" };
//        private static readonly object objetoVacio = new object();

//        /// <summary>
//        /// Establece el modo de serialización para las columnas de tipo <see cref="DateTime"/> perteneciente al actual <see cref="DataSet"/>.
//        /// </summary>
//        /// <param name="dsSource">Conjunto de datos actual.</param>
//        /// <param name="mode">Modo a establecer la serialización de fechas para las columnas.</param>
//        public static void SetDateTimeMode(this DataSet dsSource, DataSetDateTime mode = DataSetDateTime.Unspecified)
//        {
//            foreach (DataTable tabla in dsSource.Tables)

//                foreach (DataColumn columna in tabla.Columns)

//                    if (columna.DataType == typeof(DateTime))

//                        columna.DateTimeMode = mode;

//        }

//        public static bool Assert<T>(T @ref, Func<T, bool> eval)
//        {
//            return eval(@ref);
//        }

//        /// <summary>
//        /// Realiza una conversión ímplicita hacia el tipo especificado.
//        /// </summary>
//        /// <typeparam name="TOut">Tipo resultado.</typeparam>
//        public static TOut As<TOut>(this object obj)
//        {
//            return (TOut)obj;
//        }

//        ///// <summary>
//        ///// Realiza una operación atómica de asignación al objeto especificado.
//        ///// </summary>
//        ///// <returns></returns>
//        //public static TOut From<TOut>(refthis TOut obj, TOut n)
//        //{
//        //    obj = n;
//        //    return obj;
//        //}

//        /// <summary>
//        /// Realiza una operación atómica de asignación al objeto especificado.
//        /// </summary>
//        /// <returns></returns>
//        public static TOut From<TOut>(this ref TOut obj, TOut n) where TOut : struct
//        {
//            obj = n;
//            return obj;
//        }

//        /// <summary>
//        /// Devuelve un valor indicando si el objeto es o no del tipo especificado. 
//        /// </summary>
//        public static bool Is<T>(this object o)
//        {
//            return o is T;
//        }

//        /// <summary>
//        /// Devuelve un valor indicando si el objeto es nulo o no. (<see cref="Nothing"/> en VB.Net y <see cref="null"/> en C#)
//        /// </summary>
//        /// <param name="o"></param>
//        /// <returns></returns>
//        public static bool IsNull(this object o)
//        {
//            return o == null;
//        }

//        public static bool HasValue(this object o)
//        {
//            return !o.IsNull();
//        }
        
//        /// <summary>
//        /// Realiza una operación atómica de sustitución de valor asegurando un valor no vacío.
//        /// </summary>
//        public static T IfNull<T>(this object o, T replacement) where T : class
//        {
//            return (T)o ?? replacement;
//        }

//        /// <summary>
//        /// Realiza una operación atómica de sustitución de valor asegurando un valor no vacío.
//        /// </summary>
//        public static T IfDbNull<T>(this object o, T replacement) where T : class
//        {
//            return o is DBNull ? replacement : (T)o;
//        }

        

//        /// <summary>
//        /// Calcula la secuencia criptográfica <see cref="Md5"/> del archivo especificado.
//        /// </summary>
//        public static string ComputeMd5(string fName)
//        {
//            using MD5 md5Hash = MD5.Create();
//            return string.Join("", md5Hash.ComputeHash(Encoding.UTF8.GetBytes(fName)).AsParallel().Select((d) => d.ToString("x2")));
//        }

//        ///// <summary>
//        ///// Elimina un valor del registro de la subclave de iAra.
//        ///// </summary>
//        ///// <param name="key">Clave del valor a eliminar.</param>
//        ///// <param name="throwIfMissing">Establece si se lanza o no una excepción si no existe.</param>
//        //public static void RegistryDelete(string key, bool throwIfMissing = false)
//        //{
//        //    using (RegistryKey rk = Registry.CurrentUser.CreateSubKey(folderHash))
//        //    {
//        //        rk.DeleteValue(key, throwIfMissing);
//        //    }
//        //}

//        ///// <summary>
//        ///// Registra o actualiza la clave de valor especificada.
//        ///// </summary>
//        ///// <param name="key">Clave del registro</param>
//        ///// <param name="valor">Valor a establecer</param>
//        //public static void RegistryWrite(string key, string valor)
//        //{
//        //    using (RegistryKey rk = Registry.CurrentUser.CreateSubKey(folderHash))
//        //    {
//        //        rk.SetValue(key, valor);
//        //    }
//        //}

//        ///// <summary>
//        ///// Devuelve el valor la clave especificada.
//        ///// </summary>
//        ///// <param name="key">Clave del registro</param>
//        ///// <param name="defaultValue">Especifica un valor de sustitución en caso de que la clave no exista.</param>
//        ///// <returns></returns>
//        //public static string RegistryRead(string key, string defaultValue = "")
//        //{
//        //    using (RegistryKey rk = Registry.CurrentUser.CreateSubKey(folderHash))
//        //    {
//        //        return Convert.ToString(rk.GetValue(key, defaultValue));
//        //    }
//        //}

//        ///// <summary>
//        ///// Devuelve un valor indicando si la clave especificada existe o no.
//        ///// </summary>
//        ///// <param name="key">Clave del registro</param>
//        //public static bool RegistryExists(string key)
//        //{
//        //    using (RegistryKey rk = Registry.CurrentUser.CreateSubKey(folderHash))
//        //    {
//        //        return rk.GetValue(key, null) != null;
//        //    }
//        //}

//        /// <summary>
//        /// Elimina las filas en estado <see cref="DataRowState.Added"/>
//        /// </summary>
//        /// <param name="o">Tabla de datos a afectar.</param>
//        public static void DeleteAdded(this DataTable o)
//        {
//            if ((from DataColumn c in o.Columns
//                 where c.AutoIncrement && c.DataType == _int
//                 select c).Any())
//            {
//                foreach (DataRow d in o.Select("", "", DataViewRowState.Added))
//                {
//                    d.Delete();
//                    d.AcceptChanges();
//                }
//            }
//        }

//        const string newLine = @"
//";

//        /// <summary>
//        /// Devuelve una fila en formato de cadena para visualizar los valores según la versión de la fila especificada.
//        /// </summary>
//        /// <param name="version">Versión de de la fila a vizualizar.</param>
//        /// <param name="separador">Caracteres a utilizar como separador de cada tupla de columna </param>
//        /// <returns>Cadena de tuplas de valores de columna = valor.</returns>
//        /// <remarks>
//        /// <code>
//        /// #If DEBUG Then
//        ///     Console.WriteLine(_dataRow.ToString(DataRowVersion.Current, ", "))
//        /// #End if
//        /// </code>
//        /// </remarks>
//        public static string ToString(this DataRow row, DataRowVersion version = DataRowVersion.Current, string separador = "," + newLine, int indentSpaces = 0)
//        {
//            if (row.RowState == DataRowState.Deleted)
//                //Si la fila está eliminada obtenemos los originales si la fila dispone de ellos
//                if (!row.HasVersion(DataRowVersion.Original))
//                    version = DataRowVersion.Original;
//                else
//                    return "¡No tiene acceso a la información de una fila eliminada!";

//            //Luego del separador, si no sea una salto de línea, agrego una
//            if (!separador.EndsWith(newLine))
//                separador += newLine;

//            //Calculo la longitud del nombre de columna mas larga de la tabla de la fila
//            int columnaNombreLargo = (from DataColumn c in row.Table.Columns select c.ColumnName.Length).DefaultIfEmpty(0).Max() + 4;
//            //Agrego el estado de la fila y los campos con sus valores
//            return new[] { "*Estado".PadRight(columnaNombreLargo, '=') + " " + rowStateStr[row.RowState].ToString() }.Concat(
//                from DataColumn c in row.Table.Columns
//                select "".PadRight(indentSpaces, ' ') + c.ColumnName + " ".PadRight(columnaNombreLargo, '-') + " " + ObjToString(row[c, version])).Join(separador);
//        }

//        private static readonly Dictionary<DataRowState, string> rowStateStr = new ()
//        {
//            {DataRowState.Added, "Nueva"},
//            {DataRowState.Modified, "Modificada"},
//            {DataRowState.Deleted, "Eliminada(Se muestran los valores de la versión original de los datos)"}
//        };
//        /// <summary>
//        /// Devuelve una cadena con un informe de las restricciones violadas al realizar una operación en el <see cref="DataSet"/>.
//        /// </summary>
//        /// <param name="dsSource"><see cref="DataSet"/> contenedor de errores.</param>
//        /// <returns>Cadena resultado del informe.</returns> 
//        public static string GetErrors(this DataSet dsSource, int level = 0)
//        {
//            return (
//                from DataTable t in dsSource.Tables
//                where t.HasErrors
//                select t.TableName + ":" + newLine + t.GetMessageErrors(level + 1)).Join(newLine);
//        }


//        /// <summary>
//        /// Devuelve una cadena con un informe de las restricciones violadas al realizar una operación en el <see cref="DataTable"/>.
//        /// </summary>
//        /// <param name="dt"><see cref="DataTable"/> contenedor de errores.</param>
//        /// <returns>Cadena resultado del informe.</returns> 
//        public static string GetMessageErrors(this DataTable dt, int level = 0)
//        {
//            return "".PadRight(level * 4, ' ') + (
//                from DataRow dr in dt.GetErrors()
//                select FilaInfo(dr, dt, level * 4) + ": " + newLine + "".PadLeft((level + 1) * 4, ' ') + dr.RowError).Join(newLine + "".PadRight(4 * level, ' '));
//        }

//        public static string FormatearBytes(long bytes)
//        {

//            //[Pedro: 2017-09-09 16:10]

//            //   Si la cantidad de bytes es cero no hacemos ningun tipo de cálculo. 
//            //       -Si bytes = 0, Math.Log(bytes, 1024) = -2147483648, Math.Pow(bytes, 1024) = -Infinito y cualquier operación con -Infinito = NaN [Not-A-Number] (No está definido como un número).
//            //   De lo contrario realizamos las operaciones de cálculo y posterior formato de la siguiente manera:                  
//            //       -Se utiliza la siguiente fórmula (int)Math.Log(cantidadBytes, 1024) que devuelve la parte entera de la potencia de 
//            //        que a la vez es el índice con el cual indexaremos las unidades de medida de almacenamiento ya definas. 
//            //       -Si ese índice es mayor que la cantidad de unidades de medidas escogemos el último índice, sino con el índice resultado.
//            //       -Luego indexamos correctamente las unidades de medida de almacenamiento.

//            //   Ejemplo si cantidadBytes = 1354897564  
//            //       =>(int)Math.Log(cantidadBytes, 1024) = 3
//            //       =>umTamanno[umIdx] = "Gb"
//            //       La función devuelve:  "1,26 Gb"

//            //   Ejemplo si cantidadBytes = 97548523855258  
//            //       =>(int)Math.Log(cantidadBytes, 1024) = 4
//            //       !(La expresión condicional detecta que ese índice es mayor que la cantidad de elementos en la lista y 
//            //         por lo tanto se que da en que (umIdx = umTamanno.Length - 1) = 3)
//            //       =>umTamanno[umIdx] = "Gb"
//            //       La función devuelve:  "90.849,14 Gb"

//            int umIdx = 0;
//            double bytesFormateados = umIdx;
//            if (bytes > 0)
//            {
//                umIdx = Convert.ToInt32(Math.Log(bytes, 1024));
//                umIdx = umIdx < umTamanno.Length ? umIdx : umTamanno.Length - 1;
//                bytesFormateados = bytes / Math.Pow(1024, umIdx);
//            }
//            //Formatea la cifra en una cantidad
//            return string.Format("{0:#,##0.##} {1}", bytesFormateados, umTamanno[umIdx]);
//        }

//        /// <summary>
//        /// Devuelve una cadena con de cada uno de los elementos de la enumeración en formato de cadena separador por un carácter.
//        /// </summary>
//        /// <param name="separator">Separador entre cada uno de los elementos</param>
//        /// <returns></returns>
//        public static string Join(this IEnumerable source, string separator = "")
//        {
//            return string.Join(separator, from dynamic item in source select $"{item}");
//        }

//        /// <summary>
//        /// Realiza una copia en una nueva fila separada de la colección de filas de un <see cref="DataTable"/>.
//        /// </summary>
//        /// <param name="row"></param>
//        /// <returns></returns>
//        public static DataRow Copy(this DataRow row)
//        {
//            DataRow tempCopy = row.Table.NewRow();
//            tempCopy.ItemArray = row.ItemArray;
//            return tempCopy;
//        }

//        /// <summary>
//        /// Convierte una colección enumerable de elementos del tipo <typeparamref name="TIn"/> a un <see cref="DataTable"/>.
//        /// </summary>
//        /// <typeparam name="TIn">Tipo del elemento de la colección.</typeparam>
//        /// <param name="tableName">Nombre de la tabla de resultados (<see cref="DataTable.TableName"/>).</param>
//        public static DataTable ToDataTable<TIn>(this IEnumerable<TIn> source, string tableName = "Table1")
//        {
//            return source.ToDataTable((r) => objetoVacio, tableName);
//        }

//#pragma warning disable IDE0060 // Quitar el parámetro no utilizado
//        /// <summary>
//        /// Convierte una colección enumerable de elementos del tipo <typeparamref name="TIn"/> a un <see cref="DataTable"/>
//        /// </summary>
//        /// <typeparam name="TIn">Tipo del elemento de la colección.</typeparam>
//        /// <param name="tableName">Nombre de la tabla de resultados (<see cref="DataTable.TableName"/>).</param>
//        /// <param name="pk">
//        /// Define de la llave primaria de la tabla resultado según las propiedades del tipo de resultado del lambda o delegado.
//        /// </param> 
//        public static DataTable ToDataTable<TPK, TIn>(this IEnumerable<TIn> source, Func<TIn, TPK> pk, string tableName = "Table1")
//#pragma warning restore IDE0060 // Quitar el parámetro no utilizado
//        {
//            DataTable tempToDataTable = new DataTable(tableName);
//            //Creo la tabla del resultado e inicializo las variables
//            Type tInType = typeof(TIn);
//            Type? tType = null;
//            DataRow? newRow = null;
//            PropertyInfo[] propsInfo = typeof(TIn).GetProperties();
//            IEnumerator iterador = (source ?? new List<TIn>()).GetEnumerator();
//            Dictionary<string, PropertyInfo> pkDic = typeof(TPK).GetProperties().ToDictionary((p) => p.Name);
//            List<DataColumn> pkList = new List<DataColumn>();
//            bool validPkList = pkDic.Count > 0;

//            foreach (PropertyInfo prp in propsInfo)
//            {
//                //Es muy probable que las propiedades de un objeto anónimo puedan ser nulables por lo tanto no descartamos una propiedad de este tipo
//                //la siguiente instrucción obtiene el tipo subyacente de una propiedad que permita valores nulos.
//                tType = Nullable.GetUnderlyingType(prp.PropertyType);
//                //Añado la columna con el tipo correcto
//                bool isNullableColumn = tType == null;
//                tempToDataTable.Columns.Add(prp.Name, tType ?? prp.PropertyType).AllowDBNull = !prp.PropertyType.IsValueType || !isNullableColumn;
//                //Si los nombres y tipos de las propiedades son iguales y no nulos, se agrega la columna como llave primaria  
//                if (validPkList && isNullableColumn)
//                    if (tempToDataTable.Columns[prp.Name] is { } col && pkDic.TryGetValue(prp.Name, out var pInfo) && prp.PropertyType == pInfo.PropertyType)
//                        pkList.Add(col);
//                    else
//                    {
//                        pkList.Clear();
//                        validPkList = false;
//                    }
//            }

//            try
//            {
//                //Se crea la llave primaria de la table
//                if (validPkList)
//                    tempToDataTable.PrimaryKey = pkList.ToArray();
//                //Se procede a importar las filas a la tabla
//                while (iterador.MoveNext())
//                {
//                    newRow = tempToDataTable.NewRow();
//                    foreach (PropertyInfo p in propsInfo)
//                        newRow[p.Name] = p.GetValue(iterador.Current, null) ?? DBNull.Value;

//                    tempToDataTable.Rows.Add(newRow);
//                }
//            }
//            catch
//            {
//                tempToDataTable.Clear();
//            }
//            return tempToDataTable;
//        }

//        // internal class DictionaryComparer : IEqualityComparer<Dictionary<string, object>>
//        // {
//        //     bool IEqualityComparer<Dictionary<string, object>>.Equals(Dictionary<string, object> x, Dictionary<string, object> y)
//        //     {
//        //         return Equals2(x, y);
//        //     }
//        //     public bool Equals2(Dictionary<string, object> x, Dictionary<string, object> y)
//        //     {
//        //         return x.Values.SequenceEqual(y.Values);
//        //     }

//        //     int IEqualityComparer<Dictionary<string, object>>.GetHashCode(Dictionary<string, object> obj)
//        //     {
//        //         return GetHashCode2(obj);
//        //     }
//        //     public int GetHashCode2(Dictionary<string, object> obj)
//        //     {
//        //         return Convert.ToInt32(Math.Truncate(obj.Sum((k) => Convert.ToDouble(k.Value.GetHashCode()))));
//        //     }

//        // }

//        static readonly Regex noIdentifierChar = new (@"[^\w_]");

//        static string NormalizeColumnName(string colName)
//        {
//            return noIdentifierChar.Replace(colName, "");
//        }

//        private static int pivotInitializer = 1;

//        public static string GetPivotName()
//        {

//            return "Pivot" + Interlocked.Increment(ref pivotInitializer).ToString();
//        }

//        public static string ObjToString(object obj)
//        {
//            //INSTANT C# NOTE: The following VB 'Select Case' included either a non-ordinal switch expression or non-ordinal, range-type, or non-constant 'Case' expressions and was converted to C# 'if-else' logic:

//            if (obj is char || obj is string)
//                return "'" + obj.ToString() + "'";
//            else if (obj is DateTime)
//                return "#" + Convert.ToDateTime(obj).ToString(CultureInfo.InvariantCulture) + "#";
//            else if (obj is byte[] r)
//                return Convert.ToBase64String(r);
//            else
//                return Convert.ToString(obj) ?? "NULL";
//        }


//        ///// <summary>
//        ///// Genera una expresión válida para columnas calculadas de tablas de datos de .NET
//        ///// </summary>
//        ///// <param name="anno">Una cadena que representa el valor numérico constante o de referencia a otra columna numérica correspondiente a utilizar como valor del año de la expresión resultado.</param>
//        ///// <param name="mes">Una cadena que representa el valor numérico constante o de referencia a otra columna numérica correspondiente a utilizar como valor del mes de la expresión resultado.</param>
//        ///// <param name="dia">Una cadena que representa el valor numérico constante o de referencia a otra columna numérica correspondiente a utilizar como valor del día de la expresión resultado.</param>
//        ///// <returns></returns>
//        //public static string GenerarExpresionFecha(string anno = null, string mes = null, string dia = null)
//        //{
//        //    return $"{anno} + {mes}";

//        //    //Si son nulos
//        //    anno = anno ?? DateTime.Now.Year.ToString();
//        //    mes = mes ?? DateTime.Now.Month.ToString();
//        //    dia = dia ?? DateTime.Now.Day.ToString();

//        //    //Si son numéricos
//        //    anno = NumericHelper.IsNumeric(anno) ? "'" + anno + "'" : "ISNULL(" + anno + "," + DateTime.Now.Year + ")";
//        //    mes = NumericHelper.IsNumeric(mes) ? "'" + mes + "'" : "ISNULL(" + mes + "," + DateTime.Now.Month + ")";
//        //    dia = NumericHelper.IsNumeric(dia) ? "'" + dia + "'" : "ISNULL(" + dia + "," + DateTime.Now.Day + ")";

//        //    //Construyo la cadena utilizada como expresión calculada de una columna

//        //    return regex.Replace(CurrentThread.CurrentUICulture.DateTimeFormat.ShortDatePattern, (m) => ((m.Value.ToUpper()[0] == 'D') ? dia : ((m.Value.ToUpper()[0] == 'M') ? mes : anno))).Replace(CurrentThread.CurrentUICulture.DateTimeFormat.DateSeparator, "+'/'+");


//        //}

//        //static readonly Regex regex = new Regex("[y]+|[m]+|[d]+", RegexOptions.IgnoreCase);

//        /// <summary>
//        /// Comparación válida para celdas de una tabla de datos.
//        /// </summary>
//        public static bool AreEquals(object value1, object value2)
//        {
//            return Convert.IsDBNull(value1) == Convert.IsDBNull(value2) || !(Convert.IsDBNull(value1) == Convert.IsDBNull(value2)) && value1.Equals(value2);
//        }

//        public static string ToCamelCase(this string source)
//        {
//            return Regex.Replace(SustituirCaracteresLatinos(source), "\\s[\\w\\W]", (m) => m.Value.Trim().ToUpper()).Trim();
//        }

//        private static string SustituirCaracteresLatinos(string source)
//        {
//            return (source ?? "").Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ü", "u").Replace("ú", "u").Replace("ñ", "nn").Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ü", "U").Replace("Ú", "U").Replace("Ñ", "NN");
//        }

//        /// <summary>
//        /// Añade una fila a la tabla que creo su estructura u otra especificada.
//        /// </summary>
//        public static bool AddToTable(this DataRow row, DataTable? dataTable = null)
//        {
//            try
//            {
//                (dataTable ?? row.Table).Rows.Add(row);
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        /// <summary>
//        ///  Reemplaza el valor de la <paramref name="columna"/> indicada por el de  <paramref name="IfDbNullValue"/> en caso de que el valor almacenada 
//        ///  en la misma sea <see cref="DBNull.Value"/>
//        /// </summary>
//        /// <remarks>Válido para valores de una tabla de datos de .NET</remarks>
//        public static T IsNull<T>(this DataRow row, string columna, T IfDbNullValue)
//        {
//            return row.IsNull(columna) ? IfDbNullValue : (T)row[columna];
//        }

//        /// <summary>
//        /// Convierte un a cadena de "PruebaDeCadena" a "Prueba de cadena"
//        /// </summary>
//        public static string FromCamelCase(this string source)
//        {
//            return Regex.Replace(source, "[A-Z]", (m) => " " + m.Value.ToUpper()).Capitalize().Trim();
//        }

//        /// <summary>
//        /// Obtiene los valores de llaves de la fila si <see cref="DataTable.PrimaryKey"/> tiene columnas definidas
//        /// </summary>
//        /// <param name="row"></param>
//        /// <returns></returns>
//        public static object[] PrimaryKeyValues(this DataRow row)
//        {
//            return row.Table.PrimaryKey.Select((pkCol) => row[pkCol.ColumnName]).ToArray();
//        }

//        /// <summary>
//        /// Obtiene los valores de llaves de la fila si PrimaryKey tiene columnas definidas y en la versión especificada
//        /// </summary>
//        /// <param name="row"></param>
//        /// <param name="version"></param>
//        /// <returns></returns>
//        public static object[] PrimaryKeyValues(this DataRow row, DataRowVersion version)
//        {
//            return row.Table.PrimaryKey.Select((pkCol) => row[pkCol, version]).ToArray();
//        }

//        /// <summary>
//        /// Crea una tabla de datos de .NET con un informe sobre las restricciones en un Conjunto de Datos de .NET (DataSet)
//        /// </summary>
//        public static DataSet ConstraintInfo(this DataSet dsSource)
//        {

//            using DataSet ds = new DataSet("Restricciones");
//            using DataTable dt = ds.Tables.Add("Restricciones");

//            dt.PrimaryKey = new[] { dt.Columns.Add("Tabla", typeof(string)), dt.Columns.Add("Columna", typeof(string)) };

//            dt.Columns.Add("EsPrimaryKey", typeof(bool)).DefaultValue = false;
//            dt.Columns.Add("EsUnique", typeof(bool)).DefaultValue = false;
//            dt.Columns.Add("PermiteNulo", typeof(bool)).DefaultValue = false;
//            dt.Columns.Add("PerteneceRelacion", typeof(bool)).DefaultValue = false;

//            DataRow rColumna = null!;

//            foreach (DataTable t in dsSource.Tables)
//            {

//                foreach (DataColumn c in t.Columns)
//                {

//                    rColumna = dt.Rows.Find(new[] { t.TableName, c.ColumnName }) ?? dt.Rows.Add(t.TableName, c.ColumnName);
//                    rColumna["EsPrimaryKey"] = t.PrimaryKey.Contains(c);
//                    rColumna["EsUnique"] = c.Unique;
//                    rColumna["PermiteNulo"] = c.AllowDBNull;
//                    rColumna["PerteneceRelacion"] = t.Constraints.OfType<ForeignKeyConstraint>().Any((fk) => fk.Table == t && fk.Columns.Contains(c) || fk.RelatedTable == t && fk.RelatedColumns.Contains(c));
//                }

//                ds.Tables.Add(t.DefaultView.ToTable(false, t.PrimaryKey.Select((c) => c.ColumnName).ToArray()));
//            }

//            return ds;
//        }

//        /// <summary>
//        /// Convierte una cadena en minúscula excepto el primer carácter
//        /// </summary>
//        public static string Capitalize(this string source)
//        {
//            return Regex.Replace(source.ToLower(), "\\b.", (m) => m.Value.ToUpper());
//        }

//        //<Extension> Public Function ForEach(Of T)(source As IEnumerable(Of T), action As Action(Of T)) As IEnumerable(Of T)
//        //    source.ToList. _
//        //        ForEach(Function(x)
//        //                    action(x)
//        //                    Return (x)
//        //                End Function)
//        //    Return source
//        //End Function

//        /// <summary>
//        /// Proyecta una operación por cada fila de una tabla de datos de .NET
//        /// </summary>
//        /// End Function
//        public static IEnumerable<DataRow> ForEachRow(this DataTable source, Action<DataRow> action)
//        {
//            foreach (DataRow item in source.Rows)
//            {
//                action(item);
//            }
//            return source.Select();
//        }


//        /// <summary>
//        /// Actualiza el esquema de una tabla con otro esquema
//        /// </summary>
//        public static DataTable UpdateSchema(this DataTable target, DataTable source)
//        {
//            if (target.DataSet != null)
//            {
//                target.TableName = source.TableName;

//                using MemoryStream xmlStream = new();

//                source.WriteXml(xmlStream, XmlWriteMode.WriteSchema);
//                xmlStream.Position = 0;
//                target.ReadXml(new XmlTextReader(xmlStream));
//            }
//            else
//            {
//                target = source;
//            }
//            return target;
//        }

//        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
//        {
//            foreach (T item in source)
//            {
//                action(item);
//            }

//            return source;
//        }

//        public static bool IsEmpty(this InternalDataCollectionBase source)
//        {
//            return source.Count == 0;
//        }

//        public static int ForEach(this int source, Action<int> action)
//        {

//            for (int item = 0; item <= source; item++)
//            {
//                action(item);
//            }

//            return source;
//        }

//        public static IEnumerable<DataRow> ForEach(this DataTable source, Action<DataRow> action)
//        {
//            IEnumerable<DataRow> tempForEach = source.Select().AsEnumerable();

//            foreach (DataRow item in tempForEach)
//            {
//                action(item);
//            }

//            return tempForEach;
//        }

//        public static T[] ForEach<T>(this T[] source, Action<T> action)
//        {

//            foreach (T item in source)
//            {
//                action(item);
//            }

//            return source;

//        }

//        //public static System.Collections.ObjectModel.Collection<T> ForEach<T>(this System.Collections.ObjectModel.Collection<T> source, Action<T> action)
//        //{

//        //    foreach (T item in source.AsEnumerable())
//        //    {
//        //        action(item);
//        //    }

//        //    return source;
//        //}

//        public static InternalDataCollectionBase ForEach<T>(this InternalDataCollectionBase source, Action<T> action)
//        {
//            foreach (T item in source)
//            {
//                action(item);
//            }

//            return source;
//        }

//        /// <summary>
//        /// Obtiene un valor que determina si la colección contiene o no datos.
//        /// </summary>
//        public static bool HasItems(this InternalDataCollectionBase source)
//        {
//            return source.Count > 0;
//        }

//        public static short ToInt16(this string source)
//        {
//            return Convert.ToInt16(source);
//        }

//        public static int ToInt32(this string source)
//        {
//            return Convert.ToInt32(source);
//        }

//        public static long ToInt64(this string source)
//        {
//            return Convert.ToInt64(source);
//        }

//        private static string FilaInfo(DataRow r, DataTable dt, int _indentSpaces = 0)
//        {
//            DataRowVersion state = r.RowState == DataRowState.Deleted && r.HasVersion(DataRowVersion.Original) ? DataRowVersion.Original : DataRowVersion.Default;
//            return dt.PrimaryKey.Length > 0 ? "Llave primaria: (" + (
//                from c in dt.PrimaryKey
//                select "[" + c.ColumnName + "] = " + ObjToString(r[c, state])).Join(", ") + ")" : r.ToString(indentSpaces: _indentSpaces);

//        }

//        public static string UniversalTimeString()
//        {
//            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zz:00");
//        }

//        [GeneratedRegex("(?'key'[\\w_]+)(?'array'\\[\\])?(?'hasValue'\\=)?(?'value'[^&]*)?", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
//        private static partial Regex UrlParser();
//        [GeneratedRegex("charset\\s*=\\s*utf-(?<charset>\\d)", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
//        private static partial Regex CharsetParser();
//    }

//    public class Base64Converter : JsonConverter<byte[]>
//    {

//      public override bool CanConvert(Type typeToConvert) => 
//        typeof(byte[]) == typeToConvert;

//      public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
//        reader.GetBytesFromBase64();

//      public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options) => 
//        writer.WriteBase64StringValue(value);

//    }
//}