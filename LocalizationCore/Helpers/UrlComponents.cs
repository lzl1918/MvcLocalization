using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalizationCore.Helpers
{
    internal sealed class UrlComponents
    {
        private static Regex PROTOCOL_MATCHER { get; } = new Regex(@"^(?'protocol'\w+)://");

        public string Protocol { get; }
        public string Host { get; }
        public string Port { get; }
        public string CultureSpecifier { get; private set; }
        public ICultureExpression Culture { get; private set; }
        public string Action { get; }
        public string QueryString { get; }

        public string FullHost => Host == null ? null : (Port == null ? Host : $"{Host}:{Port}");
        public bool IsRelative => Protocol == null && Host == null;
        public bool HasProtocol => Protocol != null;
        public string FullAction => CultureSpecifier == null ? Action : CultureSpecifier + Action;
        public string FullActionWithQuery => string.IsNullOrEmpty(QueryString) ? FullAction : $"{FullAction}?{QueryString}";
        public string FullUrl => IsRelative ? FullActionWithQuery : (HasProtocol ? $"{Protocol}://{FullHost}{FullActionWithQuery}" : $"//{FullHost}{FullActionWithQuery}");

        private UrlComponents(string protocol, string host, string hostPort, string cultureSpecifier, string action, string queryString)
        {
            Protocol = protocol;
            Host = host;
            Port = hostPort;
            CultureSpecifier = cultureSpecifier;
            Culture = cultureSpecifier != null ? cultureSpecifier.Substring(1).ParseCultureExpression() : null;
            Action = action;
            QueryString = queryString;
        }

        public static bool TryParse(string url, out UrlComponents urlComponents)
        {
            if (TryReadProtocolAndRemain(url, out string protocol, out string host, out string port, out string cultureSpecifier, out string action, out string queryString))
            {
                urlComponents = new UrlComponents(protocol, host, port, cultureSpecifier, action, queryString);
                return true;
            }
            urlComponents = null;
            return false;
        }

        public bool TrySetCulture(string culture)
        {
            if (culture == null)
            {
                CultureSpecifier = null;
                Culture = null;
                return true;
            }

            ICultureExpression exp;
            if (culture.StartsWith('/'))
            {
                if (culture.Substring(1).TryParseCultureExpression(out exp))
                {
                    CultureSpecifier = culture;
                    Culture = exp;
                    return true;
                }
            }
            else if (culture.TryParseCultureExpression(out exp))
            {
                CultureSpecifier = $"/{culture}";
                Culture = exp;
                return true;
            }
            return false;
        }

        // read url: protocol://host:port/lang/action?query
        private static bool TryReadProtocolAndRemain(string url, out string protocol, out string host, out string port, out string cultureSpecifier, out string action, out string queryString)
        {
            Match match = PROTOCOL_MATCHER.Match(url);
            if (match.Success) // url: protocol://host:port/lang/action?query
            {
                protocol = match.Groups["protocol"].Value;
                url = url.Substring(protocol.Length + 1);
                return TryReadHostPortAndRemain(url, out host, out port, out cultureSpecifier, out action, out queryString);
            }
            else if (url.StartsWith("//"))
            {
                protocol = null;
                return TryReadHostPortAndRemain(url, out host, out port, out cultureSpecifier, out action, out queryString);
            }
            else if (url.StartsWith('/'))
            {
                protocol = null;
                host = null;
                port = null;
                return TryReadCultureAndRemain(url, out cultureSpecifier, out action, out queryString);
            }
            // what happened if directly input:
            //     action?query
            //     ?query
            protocol = null;
            host = null;
            port = null;
            cultureSpecifier = null;
            action = null;
            queryString = null;
            return false;
        }

        // referenced only by TryReadProtocolAndRemain
        // so we don't need to check input
        // read url: //host:port/lang/action?query
        private static bool TryReadHostPortAndRemain(string remain, out string host, out string port, out string cultureSpecifier, out string action, out string queryString)
        {
            remain = remain.Substring(2);
            int index = remain.IndexOf('/');
            if (index > 0)
            {
                string hostPort = remain.Substring(0, index);
                if (TryReadHostPort(hostPort, out host, out port))
                {
                    remain = remain.Substring(index);
                    return TryReadCultureAndRemain(remain, out cultureSpecifier, out action, out queryString);
                }
                else
                {
                    host = null;
                    port = null;
                    cultureSpecifier = null;
                    action = null;
                    queryString = null;
                    return false;
                }
            }
            else if (index == 0) // url: /action
            {
                // in this situation
                // the raw input is protocol:///action
                // we test it in Chrome
                // Chrome will take 'action' as the host name
                // here we simple regard it as an invalid url
                host = null;
                port = null;
                cultureSpecifier = null;
                action = null;
                queryString = null;
                return false;
            }
            else // url: host:port
            {
                if (TryReadHostPort(remain, out host, out port))
                {
                    cultureSpecifier = null;
                    action = "/";
                    queryString = null;
                    return true;
                }
                else
                {
                    host = null;
                    port = null;
                    cultureSpecifier = null;
                    action = null;
                    queryString = null;
                    return false;
                }
            }
        }

        // read url: host:port
        private static bool TryReadHostPort(string hostPort, out string host, out string port)
        {
            int index = hostPort.LastIndexOf(':');
            if (index < 0) // url: host
            {
                host = hostPort;
                port = null;
                return true;
            }
            else if (index == hostPort.Length - 1) // url: host:
            {
                host = hostPort.Substring(0, index);
                port = null;
                return true;
            }
            else if (index > 0) // url: host:port
            {
                host = hostPort.Substring(0, index);
                port = hostPort.Substring(index + 1);
                return true;
            }
            else // url: :port
            {
                host = null;
                port = null;
                return false;
            }
        }

        // read url: /lang/action?query
        private static bool TryReadCultureAndRemain(string remain, out string cultureSpecifier, out string action, out string queryString)
        {
            remain = remain.Substring(1);
            int index = remain.IndexOf('/');
            string culture = null;
            if (index < 0) // action?query
            {
                index = remain.IndexOf('?');
                if (index >= 0)
                {
                    cultureSpecifier = null;
                    return TryReadActionAndQuery(remain, out action, out queryString);
                }
                else if (remain.TryParseCultureExpression(out var _))
                {
                    cultureSpecifier = $"/{remain}";
                    action = "/";
                    queryString = null;
                    return true;
                }
                else
                {
                    action = $"/{remain}";
                    queryString = null;
                    cultureSpecifier = null;
                    return true;
                }
            }
            else if (index == remain.Length - 1) // remain: lang/
            {
                culture = remain.Substring(0, index);
                if (culture.TryParseCultureExpression(out var _))
                {
                    action = "/";
                    queryString = null;
                    cultureSpecifier = $"/{culture}";
                    return true;
                }
                else
                {
                    cultureSpecifier = null;
                    action = $"/{culture}/";
                    queryString = null;
                    return true;
                }
            }
            else if (index > 0) // remain: lang/action
            {
                culture = remain.Substring(0, index);
                if (culture.TryParseCultureExpression(out var _))
                {
                    cultureSpecifier = $"/{culture}";
                    remain = remain.Substring(index + 1);
                    return TryReadActionAndQuery(remain, out action, out queryString);
                }
                else
                {
                    // remain: action
                    cultureSpecifier = null;
                    return TryReadActionAndQuery(remain, out action, out queryString);
                }

            }
            else // index == 0, remain: /en-US/action
            {
                cultureSpecifier = null;
                return TryReadActionAndQuery(remain, out action, out queryString);
            }

        }

        // read url: action?query
        private static bool TryReadActionAndQuery(string remain, out string action, out string queryString)
        {
            int index = remain.IndexOf('?');
            if (index > 0) // remain: action?option=1
            {
                action = "/" + remain.Substring(0, index);
                queryString = remain.Substring(index + 1);
                return true;
            }
            else if (index == 0) // temp = ?option=1
            {
                action = "/";
                queryString = remain.Substring(1);
                return true;
            }
            else // temp = action
            {
                action = "/" + remain;
                queryString = null;
                return true;
            }
        }

    }
}
