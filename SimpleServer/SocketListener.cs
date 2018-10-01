// **********
// SocketListener.cs
// 
// Created on: 10.01.2018
//    Author: David Hiatt - dhiatt89@gmail.com
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// **********

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleServer
{
    public class SocketListener : IDisposable
    {
        private readonly object m_Lock = new object();
        public readonly BlockingCollection<StateObject> States = new BlockingCollection<StateObject>(new ConcurrentBag<StateObject>(), 5);
        private readonly ManualResetEvent m_allDone = new ManualResetEvent(false);

        public Log NumberLog { get; }

        public SocketListener()
        {
            NumberLog = new Log();
        }

        public void StartListening()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 4000);

            Socket listener = new Socket(ipAddress.AddressFamily,
                                         SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (!States.IsAddingCompleted)
                {
                    m_allDone.Reset();

                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                                         new AsyncCallback(AcceptCallback),
                                         listener);

                    m_allDone.WaitOne();
                    CheckAlive();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        private void AcceptCallback(IAsyncResult ar)
        {
            m_allDone.Set();

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            StateObject state;
            if(StateObject.NewState(handler, out state))
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            else
                Terminate(handler);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            string content = string.Empty;

            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                state.sb.Append(Encoding.ASCII.GetString(
                                                         state.buffer, 0, bytesRead));
                content = state.sb.ToString();
                Debug.Print("Received: '{0}'", content);
                if (content.IndexOf('\n') > -1)
                {
                    string[] allContent = content.Split(new[]{"\r\n", "\n","\r"}, StringSplitOptions.RemoveEmptyEntries);
                    state.sb.Clear();
                    foreach (string s in allContent)
                    {

                        if(s.StartsWith("terminate", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Debug.Print("Termination received");
                            Terminate();
                            break;
                        }
                        else if(s.IsValid())
                        {
                            Debug.Print("Valid string: '{0}'", s);
                            NumberLog.Add(s);
                        }
                        else
                        {
                            Debug.Print("Invalid string: '{0}'", s);
                            Terminate(state);
                            break;
                        }
                    }
                }
                else
                {
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                         new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private void CheckAlive(StateObject state = null)
        {
            if (state == null)
            {
                bool locked = false;
                try
                {
                    Monitor.Enter(m_Lock, ref locked);
                    if (locked)
                    {
                        Queue<StateObject> valid = new Queue<StateObject>();
                        while (States.TryTake(out state))
                        {
                            if (state.IsConnected())
                                valid.Enqueue(state);
                        }
                        while (valid.Count > 0 && States.TryAdd(valid.Dequeue()))
                            ;
                    }
                }
                catch { }
                finally
                {
                    if (locked)
                        Monitor.Exit(m_Lock);
                }
            }
        }

        public static void Terminate(Socket sock)
        {
            if(sock != null)
            {
                try
                {
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                    Debug.Print("Connection closed.");
                }
                catch(Exception)
                { }
            }
        }

        private void Terminate(StateObject state = null)
        {
            if (state == null)
            {
                Debug.Print("Terminating..");
                States.CompleteAdding();
                m_allDone.Set();
                while (States.TryTake(out state))
                {
                    Terminate(state);
                }
                Debug.Assert(States.IsCompleted);
            }
            else
            {
                Terminate(state.WorkSocket);
                state.Dispose();
            }
        }

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            Terminate();
            States?.Dispose();
            m_allDone?.Dispose();
            NumberLog?.Dispose();
        }

        #endregion
    }
}