# Backend Concepts — Simple Notes

These notes explain the backend ideas in this project. They use STE-100 Simplified Technical
English at a 5th-grade reading level. Each note has a JavaScript comparison where one exists.

New notes are added the first time the project uses a concept.

---

## C# and .NET

**What it is:** C# is a programming language. .NET is the set of tools that runs C# code.

**JavaScript comparison:** C# is like JavaScript. .NET is like Node.js.

**Why we use it:** Rosnet uses C# and .NET. The challenge asks for it.

**Main difference:** C# has types. You must say what kind of data each variable holds. JavaScript
does not make you do this.

---

## ASP.NET Core Minimal APIs

**What it is:** ASP.NET Core is the part of .NET that makes web servers. Minimal APIs are a short
way to write web address handlers.

**JavaScript comparison:** This is like Express.

```csharp
app.MapGet("/api/urls", () => ...);   // C#
```

```js
app.get("/api/urls", (req, res) => ...); // Express
```

**Why we use it:** Each handler is a few lines. It is easy to read.

---

## BackgroundService

**What it is:** A BackgroundService is a task that runs all the time inside the server. It starts
when the server starts. It stops when the server stops.

**JavaScript comparison:** It is like a `setInterval` that you start in your Express app.

**Why we use it:** We must check the web addresses on a schedule. Nobody clicks a button to start
the checks. This service does the work by itself.

---

## PeriodicTimer

**What it is:** A PeriodicTimer wakes up your code at a fixed time step. For example, every
5 seconds.

**JavaScript comparison:** It is like `setInterval`, but you wait for each tick with `await`.
The next tick does not start until your code is ready.

**Why we use it:** It is part of .NET. We do not need an extra package.

---

## async and await in C#

**What it is:** `async` and `await` let code wait for slow work without blocking. Slow work is
things like a web request or a database write.

**JavaScript comparison:** This is almost the same as JavaScript. A C# `Task` is like a
JavaScript `Promise`.

| C# | JavaScript |
|---|---|
| `Task<int>` | `Promise<number>` |
| `await Task.WhenAll(...)` | `await Promise.all(...)` |

---

## IHttpClientFactory

**What it is:** A helper that gives you an `HttpClient`. You use an `HttpClient` to send web
requests.

**JavaScript comparison:** An `HttpClient` is like `fetch`.

**Why we use it:** If you make a new `HttpClient` for each request, the computer can run out of
network connections. The factory shares connections in a safe way.

---

## Dependency injection

**What it is:** The server makes the objects your code needs. It gives them to your code. Your
code does not make them itself.

**JavaScript comparison:** It is like passing a `db` object into a function instead of importing
it at the top of the file. The server does the passing for you.

**Why it matters:** Tests can give your code a fake object. For example, a fake web client that
always returns "500 error".

---

## SQLite

**What it is:** A database that keeps all data in one file.

**Why we use it:** You do not need to install a database server. You clone the project and run
it. A big real system would use Postgres instead.

---

## Dapper

**What it is:** A small package. It runs SQL that you write. It turns each row into a C# object.

**JavaScript comparison:** It is like the `pg` or `better-sqlite3` packages. You write the SQL
string and get plain objects back.

**Why we use it:** We can read every query. We can explain every query to the panel.
