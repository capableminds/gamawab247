﻿using System;
using System.Collections.Generic;

namespace Domain.Common
{
    public class HttpMessageResponse<T>
    {
        public T Data { get; set; }
        public int Status { get; set; }
        public string Message { get; set; }
    }

    public class RequestHeader
    {
        public RequestHeader(IDictionary<string, string> headers)
        {
            Headers = headers;
        }
        public RequestHeader()
        { }
        public IDictionary<string, string> Headers { get; set; }
    }
}

