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

---

## The .csproj and the solution file

**What it is:** A `.csproj` file describes one C# project. It lists the .NET version and the
packages. A solution file (`HealthMonitor.slnx`) lists the projects that belong together.

**JavaScript comparison:** A `.csproj` is like `package.json`. A solution file is like an npm
workspace root. It groups several packages in one repo.

**Why we use it:** `dotnet build` at the repo root reads the solution file. It builds every
project. Later, the test project joins the same solution.

---

## appsettings.json

**What it is:** A JSON file with settings for the server. The server reads it when it starts.

**JavaScript comparison:** It is like a `config.json` file, or a `.env` file, that you read at
startup.

**Why we use it:** The database path, the check tick, and the request timeout live here. You
change a number in the file. You do not change the code.

---

## wwwroot and static files

**What it is:** `wwwroot` is a folder. The server sends the files in it to the browser as they
are. These are called static files.

**JavaScript comparison:** It is like `express.static('public')`.

**Why we use it:** Vite builds the React app into `server/wwwroot`. The same server then sends the
page and answers the API. One command runs both.

---

## MSBuild targets

**What it is:** MSBuild is the tool that builds C# projects. A target is a named step. You can
tell it to run before or after another step.

**JavaScript comparison:** It is like an npm `prebuild` script. It runs before `build`.

**Why we use it:** Our `BuildWebUi` target runs `npm install` and `npm run build` before the C#
build. A fresh clone then works with one `dotnet run`.

---

## C# records

**What it is:** A record is a small type that only holds data. You list its fields in one line.
After you make one, you cannot change its fields.

**JavaScript comparison:** It is like a plain object with a fixed shape. It is also like
`Object.freeze({ id, url })`, but the shape is checked when you build.

**Why we use it:** `CheckResult` and `UrlStatus` carry data from one place to another. Records
make that data short to write and safe to pass around.

---

## CancellationToken

**What it is:** A token that says "please stop". Long tasks check it. When someone cancels it,
they stop.

**JavaScript comparison:** It is like `AbortController` and `AbortSignal` with `fetch`.

**Why we use it:** When the app shuts down, the token is cancelled. A check that is in flight
stops. We do not save a false "unhealthy" result for it.

---

## Stopwatch

**What it is:** A clock that measures how long something takes.

**JavaScript comparison:** It is like calling `performance.now()` before and after a task, then
subtracting.

**Why we use it:** We time each check from the moment we send the request until the headers come
back. That number is the response time.

---

## HttpCompletionOption.ResponseHeadersRead

**What it is:** A setting for an HTTP request. It tells .NET to return as soon as the headers
arrive. The page body is not downloaded.

**JavaScript comparison:** It is like reading `response.status` from `fetch` and never calling
`.text()`.

**Why we use it:** We only need the status code and the time. Some pages are very big. This
setting keeps each check fast and small.
