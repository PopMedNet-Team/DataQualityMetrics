using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Models
{
    /// <summary>
    /// A response container all API calls will return.
    /// </summary>
    public class ApiResult<T> where T:class
    {
        public ApiResult() { }

        public ApiResult(T data)
        {
            Data = data;
            Errors = null;
        }

        public ApiResult(params string[] errors)
        {
            Errors = errors;
        }

        public ApiResult(T data, params string[] errors)
        {
            Data = data;
            Errors = errors;
        }

        /// <summary>
        /// A collection of error messages.
        /// </summary>
        public IEnumerable<string> Errors { get; set; }

        /// <summary>
        /// Any data the action needs to return.
        /// </summary>
        public T Data { get; set; }
    }

    public class ApiErrorResult : ApiResult<object>
    {
        public ApiErrorResult(params string[] errors) : base(errors)
        {
        }

        public ApiErrorResult(object data, params string[] errors) : base(data, errors)
        {
        }
    }
}
