﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FclEx.Extensions;
using Newtonsoft.Json;

namespace WebQQ.Im.Core
{


    public class QQException : Exception
    {
        private static readonly ConcurrentDictionary<QQErrorCode, QQException> Exceptions
            = new ConcurrentDictionary<QQErrorCode, QQException>();

        private static QQErrorCode GetErrorCode(Exception e)
        {
            e = e.InnerException ?? e;

            if (e is TimeoutException) return QQErrorCode.Timeout;
            if (e is IOException) return QQErrorCode.IoError;
            if (e is ArgumentException) return QQErrorCode.ParameterError;
            if (e is JsonException) return QQErrorCode.JsonError;
            if(e is TaskCanceledException te && !te.CancellationToken.IsCancellationRequested) return QQErrorCode.Timeout;

            var webEx = e as WebException;
            if (webEx != null)
            {
                switch (webEx.Status)
                {
                    case WebExceptionStatus.Success:
                        break;

                    case WebExceptionStatus.NameResolutionFailure:
                        return QQErrorCode.ParameterError;

                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.ReceiveFailure:
                    case WebExceptionStatus.SendFailure:
                    case WebExceptionStatus.PipelineFailure:
                        return QQErrorCode.IoError;

                    case WebExceptionStatus.Timeout:
                        return QQErrorCode.Timeout;
                    case WebExceptionStatus.UnknownError:
                        return QQErrorCode.UnknownError;

                    case WebExceptionStatus.RequestCanceled:
                    case WebExceptionStatus.ProtocolError:
                    case WebExceptionStatus.ConnectionClosed:
                    case WebExceptionStatus.TrustFailure:
                    case WebExceptionStatus.SecureChannelFailure:
                    case WebExceptionStatus.ServerProtocolViolation:
                    case WebExceptionStatus.KeepAliveFailure:
                    case WebExceptionStatus.Pending:
                    case WebExceptionStatus.ProxyNameResolutionFailure:
                    case WebExceptionStatus.MessageLengthLimitExceeded:
                    case WebExceptionStatus.CacheEntryNotFound:
                    case WebExceptionStatus.RequestProhibitedByCachePolicy:
                    case WebExceptionStatus.RequestProhibitedByProxy:
                    default:
                        return QQErrorCode.IoError;
                }
            }

            return QQErrorCode.UnknownError;
        }

        public QQErrorCode ErrorCode { get; set; }

        public static QQException CreateException(QQErrorCode errorCode)
        {
            return Exceptions.GetOrAdd(errorCode, key => new QQException(errorCode, ""));
        }

        public static QQException CreateException(QQErrorCode errorCode, string msg)
        {
            if (msg.IsNullOrEmpty()) return CreateException(errorCode);
            return new QQException(errorCode, msg);
        }

        public QQException(QQErrorCode errorCode, string msg) : base(msg)
        {
            ErrorCode = errorCode;
        }

        public QQException(Exception e) : base(e.Message, e)
        {
            ErrorCode = GetErrorCode(e);
        }

        public QQException(QQErrorCode errorCode, Exception e) : base(e.Message, e)
        {
            ErrorCode = errorCode;
        }

        public override string StackTrace => base.StackTrace ?? InnerException?.StackTrace;

        public override string Message => base.Message.RegexReplace(@"[\r\n]+", string.Empty);

        public override string ToString()
        {
            var msg = new StringBuilder($"ErrorCode={ErrorCode}, ErrorMsg={this.GetAllMessages()}, StackTrace=");
            msg.AppendLineIf($"{Environment.NewLine}{StackTrace}", StackTrace != null);
            return msg.ToString();
        }

        public string ToSimpleString()
        {
            return $"ErrorCode={ErrorCode}, ErrorMsg={Message}";
        }
    }
}
