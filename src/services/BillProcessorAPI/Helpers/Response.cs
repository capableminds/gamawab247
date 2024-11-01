﻿namespace BillProcessorAPI.Helpers
{
    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "Success";
    }
    public class SuccessResponse<T> : Response
    {
        public SuccessResponse()
        {
            Success = true;
        }
        public T Data { get; set; }
    }
    public class TokenResponse<T> : Response
    {
        public TokenResponse()
        {
            Success = true;
            IsValid = false;
        }
        public bool IsValid { get; set; }
        public T Data { get; set; }

    }
    public class ErrorResponse<T> : Response
    {
        public T Error { get; set; }
    }

    public class PagedResponse<T> : Response
    {
        public PagedResponse()
        {
            Success = true;
        }
        public T Data { get; set; }
        public Meta Meta { get; set; }
    }

    public class Meta
    {
        public Pagination Pagination { get; set; }
    }

    public class Pagination
    {
        public string NextPage { get; set; }
        public string PreviousPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }
}
