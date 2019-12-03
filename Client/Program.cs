using Grpc.Core;
using Grpc.Net.Client;
using GrpcServiceExample;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        private static Greeter.GreeterClient _greeterClient;
        static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            _greeterClient = new Greeter.GreeterClient(channel);

            // 提示调用类型
            AlertMessage();

            string cmd = Console.ReadLine();
            while (cmd != "exit")
            {
                int cmdNumber;
                try
                {
                    cmdNumber = Convert.ToInt32(cmd);
                    // 执行调用
                    await Call(cmdNumber);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(FormatException)) 
                    {
                        Console.WriteLine("命令格式错误！");
                    }
                }

                cmd = Console.ReadLine();
            }
        }

        private static void AlertMessage()
        {
            var types = Enum.GetNames(typeof(GrpcCallType));
            Console.WriteLine("请选择调用方法类型:\n");
            foreach (var typeNmae in types)
            {
                int value = (int)Enum.Parse<GrpcCallType>(typeNmae);
                Console.WriteLine(value + " :" + typeNmae + " \n");
            }
        }

        /// <summary>
        /// 调用Grpc方法
        /// </summary>
        /// <param name="cmdNumber"></param>
        /// <returns></returns>
        private static async Task Call(int cmdNumber)
        {
            switch (cmdNumber)
            {
                case (int)GrpcCallType.一元调用:
                    // 一元调用(普通请求-普通应答
                    await UnaryCall();
                    break;
                case 1:
                    // 客户端流式调用
                    await ClientStreamingCall();
                    break;
                case 2:
                    // 服务端流式调用
                    await ServerStreamingCall();
                    break;
                case 3:
                    break;
                case 4:
                    break;
                default:
                    Console.WriteLine("命令错误");
                    break;
            }
        }

        #region 一元调用

        /// <summary>
        /// 一元调用
        /// </summary>
        /// <returns></returns>
        private static async Task UnaryCall()
        {
            var call = await _greeterClient.SayHelloAsync(new HelloRequest()
            {
                Name = "一元调用先生"
            });
            Console.WriteLine("一元调用已完成,调用结果:" + call.Message);
        }

        #endregion

        #region 流式调用(客户端、服务端、双向流式调用)

        /// <summary>
        /// 客户端流式调用
        /// </summary>
        /// <returns></returns>
        private static async Task ClientStreamingCall() 
        {
            using (var call = _greeterClient.SayClientStreamingHello())
            {
                for (int i = 0; i < 3; i++)
                {
                   await call.RequestStream.WriteAsync(new HelloRequest() 
                    {
                        Name ="这是客户端流式调用写入第"+(i+1)+"个信息"
                    });
                }
                await call.RequestStream.CompleteAsync();
                var response = await call;
                Console.WriteLine("客户端流式调用完成:"+ response.Message);
            }
        }

        /// <summary>
        /// 服务端流式调用
        /// </summary>
        /// <returns></returns>
        private static async Task ServerStreamingCall() 
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(1000);

            using (var call = _greeterClient.SayServerStreamingHello(new HelloRequest() { Name = "服务端流" },cancellationToken:cts.Token))
            {
                try
                {
                    await foreach (var reply in call.ResponseStream.ReadAllAsync())
                    {
                        Console.WriteLine("接受服务端流响应:" + reply.Message);
                    }
                }
                catch (RpcException ex) when (ex.StatusCode== StatusCode.Cancelled)
                {
                    Console.WriteLine("任务取消了!");
                }
            }
            
        }

        #endregion

    }

    public enum GrpcCallType
    {
        一元调用,
        客户端流式调用,
        服务端流式调用,
        双向流式调用
    }
}
