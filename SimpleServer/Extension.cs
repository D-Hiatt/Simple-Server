// **********
// Extension.cs
// 
// Created on: 09.29.2018
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

namespace SimpleServer
{
    public static class Extension
    {
        public static bool IsValid(this string num)
        {
            if(num.Length != 9)
                return false;
            using(CharEnumerator char_enumerator = num.GetEnumerator())
            {
                while(char_enumerator.MoveNext())
                {
                    if(char_enumerator.Current < '0' || char_enumerator.Current > '9')
                        return false;
                }
            }
            return true;
        }
    }
}