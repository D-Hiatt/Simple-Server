// **********
// Log.cs
// 
// Created on: 09.28.2018
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
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SimpleServer
{
    public class Log : IDisposable
    {
        private readonly HashSet<string> m_Set = new HashSet<string>();
        private readonly Timer m_Timer;
        private int m_LastDuplicate;
        private int m_Duplicate;
        private int m_Last;


        public Log()
        {
            m_Timer = new Timer(Output, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        public void Add(string num)
        {
            if(!m_Set.Add(num))
                ++m_Duplicate;
        }

        private void Output(object ob)
        {
            Console.WriteLine("Received {0} unique numbers, {1} duplicates since last report. Unique total: {2}, duplicate total: {3}",
                              m_Set.Count - m_Last, m_Duplicate - m_LastDuplicate, m_Set.Count, m_Duplicate);
            m_Last = m_Set.Count;
            m_LastDuplicate = m_Duplicate;
        }
        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            m_Timer.Dispose();
            File.WriteAllLines("numbers.log", m_Set);
        }

        #endregion
    }
}