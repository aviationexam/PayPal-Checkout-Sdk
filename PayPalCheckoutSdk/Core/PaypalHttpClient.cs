using Microsoft.Extensions.Options;
using PayPalCheckoutSdk.Authentication;
using PayPalCheckoutSdk.Configuration;
using PayPalCheckoutSdk.Core.Interfaces;
using PayPalCheckoutSdk.RequestInterfaces;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PayPalCheckoutSdk.Core
{
    public class PayPalHttpClient : IPayPalHttpClient
    {
        private readonly HttpClient _httpClient;

        private readonly IPayPayEncoder _payPayEncoder;

        private readonly IOptions<PayPalOptions> _payPalOptions;

        public PayPalHttpClient(
            HttpClient httpClient,
            IPayPayEncoder payPayEncoder,
            IOptions<PayPalOptions> payPalOptions
        )
        {
            _httpClient = httpClient;
            _payPayEncoder = payPayEncoder;
            _payPalOptions = payPalOptions;
        }

        public PayPalOptions GetPayPalOptions => _payPalOptions.Value;

        protected virtual HttpRequestMessage CreateHttpRequest<TRequest>(
            TRequest request,
            AccessToken? accessToken = null
        )
            where TRequest : BaseHttpRequest
        {
            var httpRequest = new HttpRequestMessage(
                request.Method,
                new Uri(request.Path, UriKind.Relative)
            );

            foreach (var (header, headerValue) in request.Headers)
            {
                httpRequest.Headers.Add(header, headerValue);
            }

            if (request.Authorization != null)
            {
                httpRequest.Headers.Authorization = request.Authorization;
            }
            else if (accessToken != null)
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
            }


            return httpRequest;
        }

        protected virtual HttpContent CreateHttpContent<TRequest, TRequestBody>(
            TRequest request
        )
            where TRequest : BaseHttpRequest
        {
            if (request is IPayPalRequestWithRequestBody<TRequestBody> requestWithRequestBody)
            {
                return _payPayEncoder.SerializeRequest(requestWithRequestBody.Body, requestWithRequestBody.ContentType);
            }

            throw new ArgumentException($"The request {typeof(TRequest)} do not implement {typeof(IPayPalRequestWithRequestBody<TRequestBody>)}");
        }

        protected virtual async Task<PayPalHttpResponse> ProcessResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return new PayPalHttpResponse(response.Headers, response.StatusCode);
            }

            var responseBodyContent = await response.Content.ReadAsStringAsync(
#if NET5_0
                cancellationToken
#endif
            );

            throw new PayPalHttpException(response.StatusCode, response.Headers, responseBodyContent);
        }

        protected virtual async Task<PayPalHttpResponse<TResponse>> ProcessResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                TResponse? responseBody = default;

                if (response.Content.Headers.ContentType != null)
                {
                    responseBody = await _payPayEncoder.DeserializeResponse<TResponse>(
                        response.Content,
                        response.Content.Headers.ContentType,
                        cancellationToken
                    );
                }

                return new PayPalHttpResponse<TResponse>(response.Headers, response.StatusCode, responseBody);
            }

            var responseBodyContent = await response.Content.ReadAsStringAsync(
#if NET5_0
                cancellationToken
#endif
            );

            throw new PayPalHttpException(response.StatusCode, response.Headers, responseBodyContent);
        }

        public virtual async Task<PayPalHttpResponse<TResponse>> ExecuteAsync<TRequest, TRequestBody, TResponse>(
            TRequest request,
            AccessToken? accessToken,
            CancellationToken cancellationToken
        )
            where TRequest : BaseHttpRequest<TRequestBody>
        {
            var httpRequest = CreateHttpRequest(request, accessToken);

            if (request is IPayPalRequestWithRequestBody)
            {
                using var httpContent = CreateHttpContent<TRequest, TRequestBody>(request);
                httpRequest.Content = httpContent;
            }

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }

        public virtual async Task<PayPalHttpResponse<TResponse>> ExecuteAsync<TRequest, TResponse>(
            TRequest request,
            AccessToken? accessToken,
            CancellationToken cancellationToken
        )
            where TRequest : BaseHttpRequest
        {
            if (request is IPayPalRequestWithRequestBody)
            {
                throw new ArgumentException($"Use the {nameof(ExecuteAsync)}<TRequest, TRequestBody, TResponse> method signature");
            }

            var httpRequest = CreateHttpRequest(request, accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            return await ProcessResponseAsync<TResponse>(response, cancellationToken);
        }

        public virtual async Task<PayPalHttpResponse> ExecuteVoidAsync<TRequest, TRequestBody>(
            TRequest request,
            AccessToken? accessToken,
            CancellationToken cancellationToken
        )
            where TRequest : BaseVoidHttpRequest<TRequestBody>
        {
            var httpRequest = CreateHttpRequest(request, accessToken);

            if (request is IPayPalRequestWithRequestBody)
            {
                using var httpContent = CreateHttpContent<TRequest, TRequestBody>(request);
                httpRequest.Content = httpContent;
            }

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            return await ProcessResponseAsync(response, cancellationToken);
        }

        public virtual async Task<PayPalHttpResponse> ExecuteVoidAsync<TRequest>(
            TRequest request,
            AccessToken? accessToken,
            CancellationToken cancellationToken
        )
            where TRequest : BaseHttpRequest
        {
            if (request is IPayPalRequestWithRequestBody)
            {
                throw new ArgumentException($"Use the {nameof(ExecuteVoidAsync)}<TRequest, TRequestBody> method signature");
            }

            var httpRequest = CreateHttpRequest(request, accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            return await ProcessResponseAsync(response, cancellationToken);
        }
    }
}
