namespace DevEverywhere.CakeHttp.Inferfaces;

public interface IHttpAction
{
}


#region Empty
public interface IDelete : IHttpAction
{
    Task DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}
public interface IPost : IHttpAction
{
    Task PostAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPut : IHttpAction
{
    Task PutAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}
#endregion

#region TIn
public interface IDelete<TIn> : IHttpAction
{
    Task DeleteAsync(TIn query, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPost<TIn> : IHttpAction
{
    Task PostAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPut<TIn> : IHttpAction
{
    Task PutAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}
#endregion


#region TInOut
public interface IGet<TIn, TOut> : IHttpAction
{
    Task<TOut> GetAsync(TIn query, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}
public interface IDelete<TIn, TOut> : IHttpAction
{
    Task<TOut> DeleteAsync(TIn query, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPost<TIn, TOut> : IHttpAction
{
    Task<TOut> PostAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPut<TIn, TOut> : IHttpAction
{
    Task<TOut> PutAsync(TIn content, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPostQuery<TIn, TOut> : IHttpAction
{
    Task<TOut> PostAsync<TQuery>(TQuery query, TIn content, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPutQuery<TIn, TOut> : IHttpAction
{
    Task<TOut> PutAsync<TQuery>(TQuery query, TIn content, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}
#endregion

#region Out
public interface IGetRetrieve<TOut> : IHttpAction
{
    Task<TOut> GetAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}
public interface IDeleteRetrieve<TOut> : IHttpAction
{
    Task<TOut> DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPostRetrieve<TOut> : IHttpAction
{
    Task<TOut> PostAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}

public interface IPutRetrieve<TOut> : IHttpAction
{
    Task<TOut> PutAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
}
#endregion


//#region Empty
//public interface IDelete : IHttpAction
//{
//    Task DeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IGet : IHttpAction
//{
//    Task GetAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IPost : IHttpAction
//{
//    Task PostAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IPut : IHttpAction
//{
//    Task PutAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//#endregion

//#region Send
//public interface ISendDelete<T> : IHttpAction
//{
//    Task SendDeleteAsync(T _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface ISendPost<T> : IHttpAction
//{
//    Task SendPostAsync(T _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface ISendPut<T> : IHttpAction
//{
//    Task SendPutAsync(T _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//#endregion

//#region Update
//public interface IUpdatePost<TOut> : IHttpAction
//{
//    Task<TOut> UpdatePostAsync(TOut _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IUpdatePut<T> : IHttpAction
//{
//    Task<T> UpdatePutAsync(T _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IQueryUpdatePost<TQuery, TOut> : IHttpAction
//{
//    Task<TOut> QueryUpdatePostAsync(TQuery query, TOut _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IQueryUpdatePut<TQuery, TOut> : IHttpAction
//{
//    Task<TOut> QueryUpdatePutAsync(TQuery query, TOut _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//#endregion

//#region FilterOrSend 
//public interface IGetQueryResponse<TOut> : IHttpAction
//{
//    Task<TOut> QueryGetAndReceiveAsync<TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IDeleteQueryResponse<TOut> : IHttpAction
//{
//    Task<TOut> QueryDeleteAndReceiveAsync<TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IPostContentResponse<TOut> : IHttpAction
//{
//    Task<TOut> PostSendAndReceiveAsync<TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IPostQueryContentResponse<TOut> : IHttpAction
//{
//    Task<TOut> QueryPostAndReceiveAsync<TQuery, TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IPutQueryResponse<TOut> : IHttpAction
//{
//    Task<TOut> PutContentAndReadAsync<TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IPutQueryContentResponse<TOut> : IHttpAction
//{
//    Task<TOut> QueryPutAndReceiveAsync<TQuery, TIn>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IPostTypedContentResponse<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PostTypedSendAndReceiveAsync(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IPostTypedQueryContentResponse<TIn, TOut> : IHttpAction
//{
//    Task<TOut> QueryPostTypedAndReceiveAsync<TQuery>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IPutTypedQueryResponse<TIn, TOut> : IHttpAction
//{
//    Task<TOut> PutTypedContentAndReadAsync(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IPutTypedQueryContentResponse<TIn, TOut> : IHttpAction
//{
//    Task<TOut> QueryTypedPutAndReceiveAsync<TQuery>(TIn _in, Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//#endregion

//#region From
//public interface IFromDelete<T> : IHttpAction
//{
//    Task<T> FromDeleteAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IFromGet<T> : IHttpAction
//{
//    Task<T> FromGetAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IFromPost<T> : IHttpAction
//{
//    Task<T> FromPostAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IFromPut<T> : IHttpAction
//{
//    Task<T> FromPutAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}
//public interface IQueryPostAndReveive<T> : IHttpAction
//{
//    Task<T> QueryPostAndReceiveAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//public interface IQueryPutAndReveive<T> : IHttpAction
//{
//    Task<T> QueryPutAndReceiveAsync(Func<HttpRequestMessage, Task>? beforeSend = null, Func<HttpResponseMessage, Task>? afterSend = null);
//}

//#endregion