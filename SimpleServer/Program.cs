using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleServer
{
    public partial class AsynchronousSocketListener
        {
            private static object s_Lock = new object();
            public static BlockingCollection<StateObject> s_States = new BlockingCollection<StateObject>(new ConcurrentBag<StateObject>());
            public static ManualResetEvent allDone = new ManualResetEvent(false);

            private static Log s_Log;

            public AsynchronousSocketListener()
            {
            }

            public static void StartListening()
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 4000);

                Socket listener = new Socket(ipAddress.AddressFamily,
                                             SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);

                    while (!s_States.IsAddingCompleted)
                    {
                        allDone.Reset();

                        Console.WriteLine("Waiting for a connection...");
                        listener.BeginAccept(
                                             new AsyncCallback(AcceptCallback),
                                             listener);

                        allDone.WaitOne();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Console.WriteLine("\nPress ENTER to continue...");
                Console.Read();

            }

            public static void AcceptCallback(IAsyncResult ar)
            {
                allDone.Set();

                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
                bool locked = false;
                try
                {
                    Monitor.Enter(s_Lock, ref locked);
                    {
                        if(s_States.Count < 5)
                        {
                            StateObject state = new StateObject {workSocket = handler};
                            s_States.Add(state);
                            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                        }
                        else
                        {
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close();
                        }
                    }
                }
                catch { }
                finally
                {
                    if(locked)
                        Monitor.Exit(s_Lock);
                }
            }

            public static void ReadCallback(IAsyncResult ar)
            {
                String content = String.Empty;

                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(
                                                             state.buffer, 0, bytesRead));
                    content = state.sb.ToString();
                    if (content.IndexOf(Environment.NewLine) > -1)
                    {
                        content = content.Trim();
                        if(content.StartsWith("terminate", StringComparison.CurrentCultureIgnoreCase))
                            Terminate();
                        else if(content.IsValid())
                            s_Log.Add(content);
                    }
                    else
                    {
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                             new AsyncCallback(ReadCallback), state);
                    }
                }
            }

            private static void Terminate()
            {
                s_States.CompleteAdding();
                StateObject state;
                while(s_States.TryTake(out state))
                {
                    state.workSocket.Shutdown(SocketShutdown.Both);
                    state.workSocket.Close();
                }
            }

      /*      private static void Send(Socket handler, String data)
            {
                // Convert the string data to byte data using ASCII encoding.  
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.  
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                                  new AsyncCallback(SendCallback), handler);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.  
                    Socket handler = (Socket)ar.AsyncState;

                    // Complete sending the data to the remote device.  
                    int bytesSent = handler.EndSend(ar);
                    Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }*/

            public static int Main(String[] args)
            {
                using(s_Log = new Log())
                    StartListening();
                return 0;
            }

    }
}
