using System.Text;
using System.Collections.Generic;
using MiniJSON;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace AlSo
{
    public enum RequestType { GET, POST }

    public interface IRequestData : ISerializableData { }

    public interface IRequestFabric<REQUEST_DATA, RESPONSE_DATA> where REQUEST_DATA : IRequestData
    {
        string ApiUri { get; }
        string Method { get; }
        RequestType RequestType { get; }
        IResponseDataFacbric<RESPONSE_DATA> ResponseFabric { get; }
        IRequest<REQUEST_DATA, RESPONSE_DATA> CreateRequest(REQUEST_DATA requestData);

        async UniTask<RESPONSE_DATA> GetResult(REQUEST_DATA requestData)
        {
            IRequest<REQUEST_DATA, RESPONSE_DATA> request = CreateRequest(requestData);
            IResponse<REQUEST_DATA, RESPONSE_DATA> response = await request.GetResponse();
            RESPONSE_DATA result = response.GetResult();
            return result;
        }
    }

    public class RequestFabric<REQUEST_DATA, RESPONSE_DATA> : IRequestFabric<REQUEST_DATA, RESPONSE_DATA> where REQUEST_DATA : IRequestData
    {
        public string ApiUri { get; }
        public string Method { get; }
        public RequestType RequestType { get; }
        public IResponseDataFacbric<RESPONSE_DATA> ResponseFabric { get; }

        public RequestFabric(string apiUri, string method, RequestType requestType, IResponseDataFacbric<RESPONSE_DATA> responseFabric)
        {
            ApiUri = apiUri;
            Method = method;
            RequestType = requestType;
            ResponseFabric = responseFabric;
        }

        public IRequest<REQUEST_DATA, RESPONSE_DATA> CreateRequest(REQUEST_DATA requestData) => new Request<REQUEST_DATA, RESPONSE_DATA>(this, requestData);
    }

    public interface IRequest<REQUEST_DATA, RESPONSE_DATA> where REQUEST_DATA : IRequestData 
    {
        IRequestFabric<REQUEST_DATA, RESPONSE_DATA> RequestFabric { get; }
        REQUEST_DATA RequestData { get; }
        UniTask<IResponse<REQUEST_DATA, RESPONSE_DATA>> GetResponse();
    }


    public class Request<REQUEST_DATA, RESPONSE_DATA> : IRequest<REQUEST_DATA, RESPONSE_DATA> 
        where REQUEST_DATA : IRequestData
    {
        public IRequestFabric<REQUEST_DATA, RESPONSE_DATA> RequestFabric { get; }
        public REQUEST_DATA RequestData { get; }

        public Request(IRequestFabric<REQUEST_DATA, RESPONSE_DATA> myFabric, REQUEST_DATA requestData)
        {
            RequestFabric = myFabric;
            RequestData = requestData;  
        }

        public async UniTask<IResponse<REQUEST_DATA, RESPONSE_DATA>> GetResponse()
        {
            Dictionary<string, object> args = RequestData.GetParams();

            string jsonStringMini = Json.Serialize(args);

            byte[] reqBytes = Encoding.UTF8.GetBytes(jsonStringMini);
            UnityWebRequest request = new UnityWebRequest(RequestFabric.ApiUri + RequestFabric.Method, RequestFabric.RequestType.ToString());

            DownloadHandler dH = new DownloadHandlerBuffer();
            request.downloadHandler = dH;
            request.uploadHandler = new UploadHandlerRaw(reqBytes);
            request.useHttpContinue = false;
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            return new Response<REQUEST_DATA, RESPONSE_DATA>(RequestFabric.ResponseFabric, request, RequestData);
        }
    }


    public interface IResponse<REQUEST_DATA, RESPONSE_DATA>
    {
        bool Success { get; }
        string ErrorMessage { get; }
        string ResultMessage { get; }
        REQUEST_DATA RequestData { get; }
        IResponseDataFacbric<RESPONSE_DATA> DataFabric { get; }
        RESPONSE_DATA GetResult() => Success ? DataFabric.GetResponseData(ResultMessage) : default;
    }

    public interface IResponseDataFacbric<RESPONSE_DATA>
    {
        RESPONSE_DATA GetResponseData(string json);
    }
    
    public class Response<REQUEST_DATA, RESPONSE_DATA> : IResponse<REQUEST_DATA, RESPONSE_DATA> 
    {
        public bool Success { get; }
        public string ErrorMessage { get; }
        public string ResultMessage { get; }

        public REQUEST_DATA RequestData { get; }
        public IResponseDataFacbric<RESPONSE_DATA> DataFabric { get; }

        public Response(IResponseDataFacbric<RESPONSE_DATA> fabric , UnityWebRequest request, REQUEST_DATA data)
        {
            DataFabric = fabric;
            
            Success = request.result == UnityWebRequest.Result.Success;
            ErrorMessage = request.error;
            ResultMessage = Success ? request.downloadHandler.text : null;

            request.downloadHandler.Dispose();
            request.Dispose();

            RequestData = data;
        }
    }

    public class EmptyRequestData : IRequestData
    {
        public static EmptyRequestData Instance { get; } = new EmptyRequestData();
        private static Dictionary<string, object> Empty { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> GetParams(bool debug=false) => Empty;

        private EmptyRequestData() { }
    }
}