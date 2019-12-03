using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GrpcServiceExample
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 一元调用
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }

        /// <summary>
        /// 客户端流式调用
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<HelloReply> SayClientStreamingHello(IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context)
        {
            // 全部读取
            await foreach (var request in requestStream.ReadAllAsync())
            {
                Console.WriteLine("接收到客户端流式信息:" + request.Name);
            }

            // 逐个
            //while (await requestStream.MoveNext())
            //{
            //    Console.WriteLine("接收到客户端流式信息:" + requestStream.Current.Name);
            //}

            return new HelloReply() { Message = "ok" };
        }

        /// <summary>
        /// 服务端响应流
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task SayServerStreamingHello(HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                string requestName = request.Name;
                string msg = !string.IsNullOrWhiteSpace(requestName)
                    ? $"您好 :{ requestName} !"
                    : "未获取您的信息";
                await responseStream.WriteAsync(new HelloReply()
                {
                    Message = msg
                });

                await Task.Delay(5000);
            }

        }

        public override async Task SayBiDirectionalStreamingHello(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var name = requestStream.Current.Name;
                string msg = string.Empty;
                if (string.IsNullOrEmpty(name))
                {
                    msg = "你怎么发了个空信息过来";
                }
                else 
                {
                    msg = "我收到了你发的信息:"+name;
                }
                await responseStream.WriteAsync(new HelloReply()
                {
                    Message = msg
                });
            }
        }
    }
}
