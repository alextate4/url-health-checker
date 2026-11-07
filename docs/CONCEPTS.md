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

## .csproj and the solution file

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

---

## Query strings and Results in Minimal APIs

**What it is:** A query string is the part of a URL after the `?`. For example,
`/api/urls?window=24h` has one value. The name is `window`. The value is `24h`. In a Minimal API,
you add `string? window` to the handler. ASP.NET Core fills it from the URL. The `?` after
`string` means the value can be missing. Without it, the value is required.

`Results.BadRequest(...)` sends an HTTP 400 reply. The 400 code means the caller made a mistake.
`Results.Ok(...)` sends a 200 reply with JSON.

**JavaScript comparison:** `string? window` is like `req.query.window` in Express.
`Results.BadRequest({ error: "..." })` is like `res.status(400).json({ error: "..." })`.

**Why we use it:** The dashboard picks a window with the URL. We allow only 1h, 24h, and 7d. Any
other value gets a 400. A missing value uses 1h.

---

## SQL aggregates: COUNT, AVG and ROUND

**What it is:** An aggregate turns many rows into one number. `COUNT(*)` counts the rows.
`AVG(x)` finds the average of `x`. `ROUND(x, 1)` cuts a number to one decimal place. `AVG` skips
NULL values. It does not count them.

The `healthy` column is 1 or 0. So `AVG(healthy) * 100` is the share of healthy checks. That is
the uptime percent. If no rows match, `COUNT` gives 0 and `AVG` gives NULL. NULL becomes `null` in
JSON. The UI shows "—" for it.

**JavaScript comparison:** `COUNT(*)` is like `arr.length`. `AVG(x)` is like a `reduce` that adds
every value and divides by the count. The difference is that `AVG` ignores NULL on its own.

**Why we use it:** The database does the counting. The browser gets a few numbers, not thousands
of rows.

---

## Comparing UTC times stored as text

**What it is:** We store each check time as text, like `2026-11-07T18:30:00.0000000Z`. Text sorts
alphabetically. If every time uses the same format and the same UTC zone, alphabetical order is
also time order. So `checked_at >= @Since` works as a time filter.

We build the cutoff as the same kind of text. A C# `DateTime` parameter would be written in
another format. That format has a space and no `Z`. Compared as text, it would put old rows in the
wrong window.

**JavaScript comparison:** It is like `new Date().toISOString()`. Two ISO strings in the same
format sort in time order.

**Why we use it:** SQLite has no real date type. Plain text with one fixed format is the simplest
way to store times and filter them.

---

## xUnit tests: [Fact] and [Theory]

**What it is:** xUnit runs test methods for C#. A method with `[Fact]` is one test. A method with
`[Theory]` runs one time for each `[InlineData(...)]` line. Each line gives the method different
inputs. `Assert.Equal(expected, actual)` stops the test with a failure if the two values differ.
`dotnet test` builds the code and runs every test.

**JavaScript comparison:** `[Fact]` is like `test("name", () => { ... })` in Jest or Vitest.
`[Theory]` with `[InlineData]` is like `test.each([[200, true], [404, false]])`. `Assert.Equal` is
like `expect(actual).toBe(expected)`.

**Why we use it:** It is the standard test tool in .NET. It needs only three packages and no extra
setup.

---

## A fake HttpMessageHandler

**What it is:** `HttpClient` does not talk to the network itself. It gives each request to a
"handler". The real handler sends the request over the network. In a test, we give `HttpClient` a
fake handler. The fake handler does not use the network. It returns the answer that the test
chooses: a status code, a delay, or an error.

**JavaScript comparison:** It is like replacing `fetch` in a test with
`vi.fn(() => Promise.resolve(new Response(null, { status: 404 })))`. The code under test still
calls the same function. It does not know the answer is fake.

**Why we use it:** The tests do not depend on real websites, so they give the same result every
time. We can also make a timeout or a DNS failure happen on demand.

---

## HTTP methods and status codes: POST, PATCH, DELETE, 201, 204, 404

**What it is:** The method says what a request wants to do. `GET` reads. `POST` creates something
new. `PATCH` changes part of something. `DELETE` removes something. The server answers with a
status code. `201 Created` means a new thing was made. `204 No Content` means it worked and there
is nothing to send back. `404 Not Found` means no thing has that id.

**JavaScript comparison:** In `fetch(url, { method: 'DELETE' })`, the `method` option chooses the
method. `response.status` is the code. `response.ok` is true for any code from 200 to 299.

**Why we use it:** Anyone who knows HTTP can read our API without a manual. `DELETE /api/urls/5`
clearly removes URL 5.

---

## Database transaction

**What it is:** A transaction groups several database changes. Either all of them are saved, or
none of them are. We use one when we remove a URL. We delete its check results and the URL row
together. If the second step fails, the first step is undone too.

**JavaScript comparison:** It is like building a new array and replacing state in one
`setState` call. React never shows a half-done update. A transaction gives the database the same
"all or nothing" rule.

**Why we use it:** A removed URL never leaves half of its data behind.

---

## SemaphoreSlim: a limit on how many things run at once

**What it is:** A semaphore holds a fixed number of "slots". A task calls `WaitAsync()` to take a
slot. If no slot is free, the task waits. When the task is done, it calls `Release()`. That gives
the slot to the next waiting task. We use 4 slots, so at most 4 checks run at the same time.

**JavaScript comparison:** It is like the `p-limit` package: `const limit = pLimit(4)` and then
`limit(() => check(url))`. Only 4 promises run at once. The rest wait in a queue.

**Why we use it:** We want checks to run in parallel, but not without a limit. 100 URLs must not
open 100 connections at the same moment.

---

## Starting a task without awaiting it ("fire and forget")

**What it is:** `_ = CheckOneAsync(...)` starts an async method and does not wait for it. The loop
goes on to the next URL at once. The `_ =` tells the reader (and the compiler) that this is on
purpose. Nobody waits for the result, so the method must catch and log its own errors. If it does
not, an error is lost.

**JavaScript comparison:** It is like calling `checkOne(url)` without `await` inside a `for`
loop. Each call returns a promise that runs on its own. You need a `.catch()` inside, or a
rejection goes unhandled.

**Why we use it:** One slow URL can take 10 seconds to time out. If the loop waited for it, every
other URL would wait too.
