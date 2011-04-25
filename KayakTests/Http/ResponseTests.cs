﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Kayak.Http;
using Kayak;

namespace KayakTests.Http
{
    [TestFixture]
    public class ResponseTests
    {
        Response response;
        MockOutputStream output;
        MockRequest request;

        class MockRequest : IHttpServerRequest
        {
            public event EventHandler<DataEventArgs> OnBody;
            public event EventHandler OnEnd;

            public string Method { get; set; }
            public string Uri { get; set; }
            public Version Version { get; set; }

            public IDictionary<string, string> Headers { get; private set; }

            public MockRequest()
            {
                Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        [SetUp]
        public void SetUp()
        {
            output = new MockOutputStream();
            request = new MockRequest();
        }

        void WriteBody(string data)
        {
            response.WriteBody(new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)), null);
        }

        [Test]
        public void OneOh__End_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, false);

            response.End();

            Assert.That(output.Buffer.ToString(), Is.Empty);
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }

        [Test]
        public void OneOhKeepAlive__End_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, true);

            response.End();

            Assert.That(output.Buffer.ToString(), Is.Empty);
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }

        [Test]
        public void OneOh__WriteHeaders_End_writes_headers_and_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, false);

            response.WriteHeaders("200 OK", new Dictionary<string, string>() {
                { "Date" , "today" }, { "Server", "Kayak" }
            });
            response.End();

            Assert.That(output.Buffer.ToString(), Is.EqualTo(@"HTTP/1.0 200 OK
Date: today
Server: Kayak

"));
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }

        [Test]
        public void OneOhKeepAlive__WriteHeaders_End_writes_headers_and_ends_output_keep_alive()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, true);

            response.WriteHeaders("200 OK", new Dictionary<string, string>() {
                { "Date" , "today" }, { "Server", "Kayak" }
            });
            response.End();

            Assert.That(output.Buffer.ToString(), Is.EqualTo(@"HTTP/1.0 200 OK
Date: today
Server: Kayak
Connection: keep-alive

"));
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.True);
        }

        [Test]
        public void OneOh__WriteHeaders_connection_close_End_writes_headers_and_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, false);

            response.WriteHeaders("200 OK", new Dictionary<string, string>() {
                { "Date" , "today" }, { "Server", "Kayak" }
            });
            response.End();

            Assert.That(output.Buffer.ToString(), Is.EqualTo(@"HTTP/1.0 200 OK
Date: today
Server: Kayak

"));
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }

        [Test]
        public void OneOhKeepAlive__WriteHeaders_connection_close_End_writes_headers_and_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, true);

            response.WriteHeaders("200 OK", new Dictionary<string, string>() {
                { "Date" , "today" }, { "Server", "Kayak" }, { "Connection", "close" }
            });
            response.End();

            Assert.That(output.Buffer.ToString(), Is.EqualTo(@"HTTP/1.0 200 OK
Date: today
Server: Kayak
Connection: close

"));
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }

        [Test]
        public void OneOh__WriteHeaders_WriteBody_End_writes_headers_and_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, false);

            response.WriteHeaders("200 OK", new Dictionary<string, string>() {
                { "Date" , "today" }, { "Server", "Kayak" }
            });

            WriteBody("hello");
            response.End();

            Assert.That(output.Buffer.ToString(), Is.EqualTo(@"HTTP/1.0 200 OK
Date: today
Server: Kayak

hello"));
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }

        [Test]
        public void OneOhKeepAlive__WriteHeaders_WriteBody_End_writes_headers_and_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, true);

            response.WriteHeaders("200 OK", new Dictionary<string, string>() {
                { "Date" , "today" }, { "Server", "Kayak" }
                // XXX really there would have to be content length. require this?
            });

            WriteBody("hello");
            response.End();

            Assert.That(output.Buffer.ToString(), Is.EqualTo(@"HTTP/1.0 200 OK
Date: today
Server: Kayak
Connection: keep-alive

hello"));
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.True);
        }

        [Test]
        public void OneOh__WriteHeaders_WriteBody2x_End_writes_headers_and_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, false);

            response.WriteHeaders("200 OK", new Dictionary<string, string>() {
                { "Date" , "today" }, { "Server", "Kayak" }
            });

            WriteBody("hello");
            WriteBody("world");
            response.End();

            Assert.That(output.Buffer.ToString(), Is.EqualTo(@"HTTP/1.0 200 OK
Date: today
Server: Kayak

helloworld"));
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }

        [Test]
        public void OneOhKeepAlive__WriteHeaders_WriteBody2x_End_writes_headers_and_ends_output()
        {
            request.Version = new Version(1, 0);
            response = new Response(output, request, false);

            response.WriteHeaders("200 OK", new Dictionary<string, string>() {
                { "Date" , "today" }, { "Server", "Kayak" }
            });

            WriteBody("hello");
            WriteBody("world");
            response.End();

            Assert.That(output.Buffer.ToString(), Is.EqualTo(@"HTTP/1.0 200 OK
Date: today
Server: Kayak

helloworld"));
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }

        [Test]
        public void OneOne_End_ends_output()
        {
            request.Version = new Version(1, 1);
            response = new Response(output, request, true);

            response.End();

            Assert.That(output.Buffer.ToString(), Is.Empty);
            Assert.That(output.GotEnd, Is.True);
            Assert.That(response.KeepAlive, Is.False);
        }
    }
}