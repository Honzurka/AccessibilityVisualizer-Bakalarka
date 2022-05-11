# tutorials
- microsoft
    - https://docs.microsoft.com/en-us/learn/browse/?expanded=dotnet&products=aspnet-core
    - https://docs.microsoft.com/cs-cz/aspnet/core/?utm_source=aspnet-start-page&utm_campaign=vside&view=aspnetcore-6.0

# Microsoft docs notes
- UI
    - server rendered
        - **Razor Pages** (recommended for new devs)
    - client rendered: DOM updates (JS)
        - Blazor
        - Single page
## Tutorials
### Web apps/Razor Pages
- Folders
    - Pages
        - .cshtml: C# + HTML (Razor syntax)
            - `@page`
                - MVC action => can handle requests
                - must be 1st directive
                - ex. @page "{id:int}"
                    - `href="/Movies/Edit?id=1"` => `href="/Movies/Edit/1"`
            - `@model`
                - makes model available
            - HTML helpers
                - `@Html.DisplayNameFor`
                - `@Html.DisplayFor`
            - tag helpers
                - form method
                    - automatically includes `antiforgery token`
                - validation: display validation errors
                    - `<div asp-validation-summary`
                    - `<span asp-validation-for`
                - label
                    - `<label asp-for="Movie.Title" class="control-label"></label>`
                        - label caption + [for] attrib for Title prop.
                - input
                    - `<input asp-for="Movie.Title" class="form-control">`
                        - uses DataAnnotations attrib + produces HTML attrib for jQuery validation on client
                - anchor
                    - `asp-page="./Edit" asp-route-id="@item.ID"`
                        - dynamically generates `href="/Movies/Edit?id=1"`
        - .cshtml.cs: C# handling page events
            - retval `void/Task` => no return
        - _* supporting file
            - _Layout.cshtml
                - `@RenderBody`: placeholder for page-specific view
                - `@ViewData`: dict, used to pass data to view
    - wwwroot: static assets
    - appsettings.json
    - program.cs
    - startup.cs
- razor comment: `@* *@` - not sent to client
- data annotations
    - `Display(Name = "...")`
- concurrency exception handling
    - in generated OnPostAsync()
- attributes
    - [BindProperty]
        - binds form vals + query strings as property
        - SupportsGet - bind on get
- validation
    - in model
    - both client and server side
    - DataAnnotations
        - [Required], [StringLength], [RegularExpression] [Range] for validation
        - [Datatype] for formatting
            - also provides semantics => more powerful than [DisplayFormat]
    - changes DB schema

## Fundamentals
- startup ------------------------ spousteni service-spise jsem nenasel nic o custom -- ??adding custom library to ASP??
    - services configured
    - request handling pipeline defined => middleware
- dependency injection (DI service != web service)    -------- muze se hodit pro zpristupneni vlastni service (RAPTOR)
    - ctor injection
        - class declares ctor with type/iface
        - DI fmwk provides instance at runtime
    - pouziti
        - ConfigureServices(): `services.AddScoped<IMyDependency, MyDependency>();`
            - addScoped souvisi s `lifetime` => pro 1 request
        - ziskani service skrze `ctor`
            - zavislosti na Iface, ne implementaci
            - nevytvari instanci
    - casto chainovani dependenci: kazda dependence si vyzada vlastni dependence
    - [registrace skupiny souvisejicich services](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-5.0#register-groups-of-services-with-extension-methods-1)
    - service lifetime
        - transient: always different
        - **scoped**: same for 1 request, different for new request
        - **singleton**: same for every requiest
    - designing services for DI
        - avoid stateful/static classes and members
            - instead design app to use singleton
        - avoid instantiation of dependencies inside service
        - make services small

- middleware
    - composed in pipeline using `app.Use...()` in Startup.Configure
- host
    - web server cfg
- servers
    - Kestrel - cross-platform
- configuration
- environments
    - development / production
- logging
- routing
    - route == URL mapped to handler
- error handling
- HTTP request
- content root
- web root == contentRoot/wwwroot
    - pointed by `~/`
- [static files](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-5.0)
    - todo

## Advanced
### Model binding
- automatically converts types from string to target type
    - custom conversion => use string type (allows custom conversion error handling)
- after property bound => model validation
    - ModelState - contains info about bound data
        - .IsValid - successful validation
- targets (to which values are bound)
    - action method params
    - handler method params
    - public properties - if specified by attrib
        - [BindProperty]
        - [BindPropeties]
            - applied to class
            - binds all propeties of its class
        - HTTP GET: doesn't bind props by default
            - [BindProperty(SupportsGet = true)] 
- sources of values
    - `Form fields`, `request body`, route Data, `query string params`, uploaded files
        - route data, query strings: only for **simple types**
        - uploaded files: bound only to targets implementing IFormFile
    - priority can be changed using [From(Query/Route/...)]
        - [FromBody] - delegated to `input formatter`
            - if complex, its properties are bound ONLY from body
            - MAX for 1 param per action method
    - additional sources: custom provider
        - from cookies, ...
- no source => no error (just null/default vals) unless [BindRequired]
- type conversion error == invalid state
- [simple types](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-5.0#simple-types-1)
    - model binder can converts string to these
- complex types
    - must have
        - public default ctor
        - public writable props
    - property binding through
        1. prefix.prop_name / [Bind(prefix = "...")]
            - prefix == param name (binding param) | prop name (binding prop)
        2. just prop_name
    - attribs
        - [Bind("prop1,prop2,...")] - which properties should be bound
        - [ModelBinder]
            - can specify type of model binder
            - [ModelBinder(Name = "newName)] change name of property
        - [BindRequired] only for props, error if not bound
        - [BindNever] only for props
- collections
    - collections of simple types
        - looks for param/prop name or supported format without prefix (indexes)
        - don't use `index` name
- dictionaries
    - probably for simple types only
- special data types
    - FormCollection: retrieve all values from posted form data
- input formatters
    - request body data in many formats
    - default JSON-based formatter
        - different from NuGet
    - can be cutomized
- manual model binding
    - using TryUpdateModelAsync()
        - values from form body, query string or route data

## Session
- HttpContext.Session
- extension methods for ISession in Microsoft.AspNetCore.Http
- session data must be serialized / str / int

## Validation
- reurunning validation - vhodne po napr. po prepoctu nejakych atributu
    ```c#
    ModelState.ClearValidationState()
    TryValidateModel()
    ```
- validation attribute
    - could be custom
    - required: only for nullable args (ex. `decimal?`)
- client-side
    - with `tag helpers` and `HTML helpers` thanks to jQuery
- [validation of dynamic forms](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-5.0#add-validation-to-dynamic-forms-1)
    - `$.validator.unobtrusive.parse()` - passes data-* attribs to jQuery Validation


- moznosti?
    - json schema: https://khalidabuhakmeh.com/using-dotnet-to-validate-json-with-json-schema
    - custom model binder: https://stackoverflow.com/questions/55154252/how-to-validate-json-request-body-as-valid-json-in-asp-net-core
    - fluent validation: https://www.c-sharpcorner.com/article/validata-a-json-list-of-objects-in-asp-net-core-using/

## Other
- Tag helpers
    - https://docs.microsoft.com/en-us/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-6.0#the-input-tag-helper
        - search for `collection`
            - Html.Editor and Html.EditorFor handle collections, complex objects and templates; the Input Tag Helper doesn't
            - NOT SURE HOW TO DO IT
                - sending json might be better
                    - https://stackoverflow.com/questions/12042476/normal-form-submission-vs-json
                    - https://stackoverflow.com/questions/8604717/json-vs-form-post/8604798
                        - JS FormData ?
- javascript VS @Html.EditorFor(m => m.Colors[index]) + umele instance
- "dynamic form" https://stackoverflow.com/questions/38795210/how-to-create-a-form-with-a-variable-number-of-inputs



1. predvytvorit si form a pouzit hide/show
2. posilat JSON data na server
react je asi jednodussi

# Learn/Create a web UI with ASP.NET Core
- C# in razor pages is run before page is sent to client
- `page model` == Pages/pageName.cshtml.**cs**
    - defines data properties
    - encapsulates logic/ops related to these data
        - handlers for HTTP requests
- Pages/Shared
    - shared across several pages
- layout == .cshtml
    - shared
- partial view
    - break up large markup files into smaller
- Pages dir
    - default for routing
- Services
    - ???-----------
- tag helpers
    - label `<label asp-for="PageModelProperty"`
        - extension of HTML label, 
    - input `<input asp-for="PageModelProperty"`
        - HTML id, name based on C# property
        - type based on property type
            - bool -> checkbox
        - client-side validation based on attributes in PageModel
        - enforces server-side validation
    - partial
    - validation summary: `<div asp-validation-summary="All"></div>`
        - displays validation msg (in case of incorrect input)




