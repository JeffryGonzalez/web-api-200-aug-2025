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

**Your Thinking and Examples Below**

> I think we should support `POST` on a new `/employee/problems/{problemId}/cancel` endpoint, but only allow cancellation if the problem is still in the `AwaitingTechAssignment` status.

That might look like this:

```http
POST http://localhost:1337/employee/problems/12345/cancel
Accept: application/json
Authorization: Bearer {JWT}
```

And could return a confirmation that the problem was cancelled. For example:

```http
200 Ok
Content-Type: application/json

{
    "problemId": 12345,
    "status": "Cancelled"
}
```

I added the authorization header because we need to verify the employee is cancelling their own problem.

> What if the problem is not in the `AwaitingTechAssignment` status?

We should return a 400 Bad Request, with a message explaining why cancellation is not allowed.

For example:

```http
400 Bad Request
Content-Type: application/json

{
    "error": "Problem cannot be cancelled. Current status: AssignedToTech"
}
```

> What if the employee tries to cancel a problem that doesn't exist or isn't theirs?

We should return a 404 Not Found, or a 403 Forbidden if they don't have permission.

For example:

```http
404 Not Found
Content-Type: application/json

{
    "error": "Problem not found"
}
```
or
```http
403 Forbidden
Content-Type: application/json

{
    "error": "You do not have permission to cancel this
}
```

### Assigning a Problem To A Tech

> We need a way for the techs of the Help Desk to see a list of *all* problems that are awaiting tech assignment. It might be nice if they could filter by either the person that submitted them, or by the software they are having an issue with.

**Your Thinking and Examples Below**

> I think we should support `GET` on a new `/tech/problems` endpoint that returns all problems with the status `AwaitingTechAssignment`. Techs could filter by the employee who submitted the problem or by the software involved using query string arguments.

That might look like this:

```http
GET http://localhost:1337/tech/problems?submittedBy=employee123&softwareId=398398938
Accept: application/json
Authorization: Bearer {JWT}
```

And could return an array of problems matching the filters. For example:

```http
200 Ok
Content-Type: application/json

[
    ... each of the problems here ...
]
```

I added the authorization header because only techs should be able to access this endpoint.

> Once a tech has "located" a problem that is awaiting tech assignment, how could, through the API, that tech "adopt" that? What would happen to that problem?

We could support `POST` on `/tech/problems/{problemId}/assign` to let a tech claim a problem.

For example:

```http
POST http://localhost:1337/tech/problems/12345/assign
Accept: application/json
Authorization: Bearer {JWT}
```

And could return a confirmation that the problem was assigned to the tech:

```http
200 Ok
Content-Type: application/json

{
    "problemId": 12345,
    "assignedTo": "tech789",
    "status": "AssignedToTech"
}
```

> What if the problem is already assigned to another tech?

We should return a 400 Bad Request with a message explaining why assignment is not allowed.

For example:

```http
400 Bad Request
Content-Type: application/json

{
    "error": "Problem is already assigned to another tech."
}
```

> What if the tech tries to assign a problem that doesn't exist?

We should return a 404 Not Found.

For example:

```http
404 Not Found
Content-Type: application/json

{
    "error": "Problem not found"
}
```

> Once a tech has "located" a problem that is awaiting tech assignment, how could, through the API, that tech "adopt" that. What would happen to that problem?
>

**Your Thinking and Examples Below**

> I think we should support `POST` on `/tech/problems/{problemId}/assign` to let a tech "adopt" a problem that is awaiting tech assignment.

That might look like this:

```http
POST http://localhost:1337/tech/problems/12345/assign
Accept: application/json
Authorization: Bearer {JWT}
```

And could return a confirmation that the problem was assigned to the tech:

```http
200 Ok
Content-Type: application/json

{
    "problemId": 12345,
    "assignedTo": "tech789",
    "status": "AssignedToTech"
}
```

I added the authorization header because only authenticated techs should be able to assign themselves to a problem.

> What would happen to that problem?

The problem's status would change from `AwaitingTechAssignment` to `AssignedToTech`, and the tech's identifier would be recorded as the person responsible for resolving it. This prevents other techs from claiming the same problem.

> What if the problem is already assigned to another tech?

We should return a 400 Bad Request with a message explaining why assignment is not allowed.

For example:

```http
400 Bad Request
Content-Type: application/json

{
    "error": "Problem is already assigned to another tech."
}
```

> What if the tech tries to assign a problem that doesn't exist?

We should return a 404 Not Found.

For example:

```http
404 Not Found
Content-Type: application/json

{
    "error": "Problem not found"
}
```