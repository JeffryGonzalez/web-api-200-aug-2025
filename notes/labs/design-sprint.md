# Design Sprint Lab

The purpose here is to have you *think* about good API design, using some of the principals we've covered in this class. 

For each of the proposed requirements, provide (in the space provided) you thinking on how best to expose this on the API. Use sample `HTTP` requests, where possible. 

The "required" part is you provide *some* idea of how this would look.

If you'd like, you can implement some or all of this in the API.

## An Example

**Allowing An Employee To See All Their Problems**

Right now, on the HelpDesk.Api, employees can *POST* a problem, and retrieve that problem.

We need a way for the employee to see a list of *all* of their Problems.

**Your Thoughts Here**

> I think we should just support `GET` on the existing `/employee/problems` endpoint.

That might look like this:

```http
GET http://localhost:1337/employee/problems
Accept: application/json
Authorization: Bearer {JWT}
```

And could return an array of their problems. For Example:

```http
200 Ok
Content-Type: application/json

[
    ... each of the problems here
]
```

I added the authorization header because that's how we'd know which employee to get the data for.

> What if the employee has no problems?

We should return a 200 Ok from the call, and an empty array.

> What if we wanted to allow the employee to filter the problems, but only those for a specific piece of software?

We could use a QueryString argument.

For example:
```http
GET http://localhost:1337/employee/problems?softwareId=398398938
Accept: application/json
Authorization: Bearer {JWT}
```

## "Real" Features

### Cancelling a Problem 

> We have found that sometimes an employee "figures out" their problem before we even have a chance to assign this to a tech. We need an endpoint that will allow the employee to say "nevermind about that one". But it should only allow that if the problem is still `AwaitingTechAssignment`.

We could possibly add an AwaitingTechAssignment endpoint to the API, this will allow the tech to retreive the problems that still need to be assigned a tech to be assigned, but we can add within the response a boolean that could state if the problem needs attention (true) or state that the problem has been solved by the employee (false).

we could make use of GET and may look like this...
```http
GET http://localhost:1337/employee/problems/AwaitingTechAssignment
Accept: application/json
```

We could get a response that may look like this...
```http
200 Ok
Content-Type: application/json

[
    {
        // We get a response of a each problem still awaiting assignment along with stating
        // if ithas been solved by employee or not
        SolvedByEmplyee: "True" 
    },
    {
        // This would state that the problem still needs an assignment
        SolvedByEmployee: "False"

    }
]
```

**Your Thinking and Examples Below**

### Assigning a Problem To A Tech

> We need a way for the techs of the Help Desk to see a list of *all* problems that are awaiting tech assignment. It might be nice if they could filter by either the person that submitted them, or by the software they are having an issue with.

Similar to a enhancment as stated above, we could also make user of a QueryString to filter by the EmployeesId and only be able to see the problems pertaining to that specific employee

We can have the problems employee with the query string...
```http
GET http://localhost:1337/employee/problems?EmployeeId=some#
Accept: application/json
```

In return we can get a response of the filtered Employee. Which can look like...
```http
200 Ok
Content-Type: application/json

[
    // CEO problems
]
```

```http
200 Ok
Content-Type: application/json

[
    // Developer problems
]
```

**Your Thinking and Examples Below**

> Once a tech has "located" a problem that is awaiting tech assignment, how could, through the API, that tech "adopt" that. What would happen to that problem?
>

We could add a POST "TechAssignment" endpoint to the API along with a QueryString so they can know who took that problem that needed a tech assigned. The query string could either be the name of the tech or a tech ID. It would look something like...
```http
POST http://localhost:1337/employee/problems/techAssignment?TechName=someName
Accept: application/json
```

The Response could look like...
```http
200 Ok
Content-Type: application/json

[
    // The problem along with the assigned tech name
    TechName: "johnny"
]
```

OR
```http
POST http://localhost:1337/employee/problems/techAssignment?TechId=some#
Accept: application/json
```

The Response could look like...
```http
200 Ok
Content-Type: application/json

[
    // The problem along with the assigned tech ID
    TechName: "U189877"
]
```




**Your Thinking and Examples Below**

