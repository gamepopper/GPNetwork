using System;
using System.Runtime.Serialization;

/*
 * Written by Tim Stoddard
 * Multiplayer Games & Software Engineering
 * Staffordshire University
 */

namespace GPNetworkMessage
{
    [Serializable]
    public class AMessage
    {
        public AMessage(MessageType Type, int ID, string Message) { this.Type = Type; this.ID = ID; this.Message = Message; }
        public MessageType Type = MessageType.ANY;
        public int ID = 0;
        public string Message = "";
    }

    public enum MessageType
    {
        ANY,
        JOIN,
        LEAVE,
        INFO,
        INFOTO1,
        INFOEXCEPT1,
        ERROR,
        CLIENTCOUNT
    }
}

/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 Tim Stoddard
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */