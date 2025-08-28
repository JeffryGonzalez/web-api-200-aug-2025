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
GET http://localhost:1337/employee/problems?softwareId=bc3f26b0-4f07-41ad-837d-e73733afbe83
Accept: application/json
Authorization: Bearer {JWT}
```

## "Real" Features

### Cancelling a Problem 

> We have found that sometimes an employee "figures out" their problem before we even have a chance to assign this to a tech. We need an endpoint that will allow the employee to say "nevermind about that one". But it should only allow that if the problem is still `AwaitingTechAssignment`.

**Your Thinking and Examples Below**

I think we could just do a DELETE to the resource. 

```http
GET http://localhost:1337/employee/problems/2b533aef-c1f1-4960-be4c-fb5cce868a64
```

```http
DELETE http://localhost:1337/employees/problems/2b533aef-c1f1-4960-be4c-fb5cce868a64
```

- If the problem with that Id:
    - exists
    - is still awaiting tech assignment
- Then return a 204 - No Content
    - Behind the scenes, either delete it from the database, or mark it as cancelled.
    - If we don't delete it, make sure it doesn't show up through the API any longer.

- If a problem with that Id does not exist, return a 204 - No Content
- If a problem with that Id does exist, but it is not in the "AwaitingTechAssignment" state, send a 409 - Conflict



### Assigning a Problem To A Tech

> We need a way for the techs of the Help Desk to see a list of *all* problems that are awaiting tech assignment. It might be nice if they could filter by either the person that submitted them, or by the software they are having an issue with.

**Your Thinking and Examples Below**

I propose a new resource:

```http
GET http://localhost:1338/techs/problem-queue
Accept: application/json
Authorization: Bearer {JWT with Role="HelpDeskTech"}
```

This should be secured so that only members of the "HelpDeskTech" role can access this.

We could add QueryString arguments for the filtering.

```http
GET http://localhost:1338/techs/problem-queue?softwareId={idOfSoftware}&submittedBy={who}
Accept: application/json
Authorization: Bearer {JWT with Role="HelpDeskTech"}
```



> Once a tech has "located" a problem that is awaiting tech assignment, how could, through the API, that tech "adopt" that. What would happen to that problem?


**Your Thinking and Examples Below**

The tech could put that in their basket of current work:

```http
PUT http://localhost:1338/techs/current-work/{idOfIssue}
Accept: application/json
Authorization: Bearer {JWT with Role="HelpDeskTech"}
```

If the issue is still `AwaitingTechAssignment` this will change the status to `AssignedToTech` and return an `202 Accepted` status code.

It should no longer appear in the problem queue.

It *should* appear in the tech's `current-work`.

```http
GET http://localhost:1338/techs/current-work
Accept: application/json
Authorization: Bearer {JWT with Role="HelpDeskTech"}
```


