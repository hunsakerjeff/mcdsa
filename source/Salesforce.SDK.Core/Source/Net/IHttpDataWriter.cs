using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Salesforce.SDK.Net
{
    public interface IHttpDataWriter
    {
        Task PushToStreamAsync(
            Func<IOutputStream, IAsyncOperationWithProgress<UInt64, UInt64>> writeToStreamAsync, 
            IHttpContent content);
    }
}
