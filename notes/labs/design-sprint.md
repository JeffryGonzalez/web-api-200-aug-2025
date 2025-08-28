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

I think an employee should be able to cancel their request. (Likely in both queue's, but for the purposes of the lab I will focus on the Awaiting Assignment) I would make a push for this to not be a delete, but just another state of a request, like 'Cancelled'. This would allow us to preserve the data and compare to how often they are cancelled. And gain more knowledge around cancelled. However, I am going to tackle this in a simple state and will use the delete method.

DELETE http://localhost:1337/employee/problems/{problemId}
Accept: application/json
Authorization: Bearer {JWT}

If the cancel worked:
200 Ok
Content-Type: application/json

{
    "message": "Problem successfully cancelled."
}

If the cancel failed due to it not being in AwaitingTechAssignment:
400 Bad Request
Content-Type: application/json

{
    "error": "Problem cannot be canceled because a Tech has already been assigned."
}

I would love to introduce a secondary alert here. Where the tech that is assigned would recieve a ping that there was an attempted cancel and to contact the employee. And thus requiring the tech to close the problem. This would also cause undo work. Which is why I stand behind the value of allowing a cancel at any point and recording the cancel, rather than deleting it.

This could be done by creating a whole seperate end point via a POST.

And finally if they are not the correct employee for the problem:
403 Forbidden
Content-Type: application/json

{
    "error": "You are not authorized to cancel this problem. Please do not try again. I promise it won't work."
}

### Assigning a Problem To A Tech

> We need a way for the techs of the Help Desk to see a list of *all* problems that are awaiting tech assignment. It might be nice if they could filter by either the person that submitted them, or by the software they are having an issue with.

**Your Thinking and Examples Below**

I think techs should have the ability to retrieve a list of all problems that are in the AwaitingTechAssignment state. This would allow them to see what needs to be worked on, and optionally filter the list based on specific criteria (like the person who submitted the problem or the software involved). This would help prioritize tasks and improve efficiency.

For simplicity, I would create a GET endpoint that allows techs to retrieve all problems in the AwaitingTechAssignment state. To make it more useful, I’d add query parameters for filtering.

Here’s an example of the endpoint:

GET http://localhost:1337/tech/problems?status=AwaitingTechAssignment
Accept: application/json
Authorization: Bearer {JWT}

If the tech wants to filter by the employee who submitted the problem:

GET http://localhost:1337/tech/problems?status=AwaitingTechAssignment&employeeId=12345
Accept: application/json
Authorization: Bearer {JWT}

If the request is successful, the server would return a 200 response with a list of problems:

200 Ok
Content-Type: application/json

[
    {
        "problemId": "67890",
        "employeeId": "12345",
        "description": "I was in a meeting and not paying attention",
        "status": "AwaitingTechAssignment"
    },
    {
        "problemId": "67891",
        "employeeId": "12345",
        "description": "What is this ping I get when I get an email",
        "status": "AwaitingTechAssignment"
    }
]

If no problems match the filters, the server would return:
200 Ok
Content-Type: application/json

[]

If the tech is unauthorized (e.g., they’re not logged in or don’t have the right permissions):
403 Forbidden
Content-Type: application/json

{
    "error": "Get your grubby hands off someone elses work."
}


> Once a tech has "located" a problem that is awaiting tech assignment, how could, through the API, that tech "adopt" that. What would happen to that problem?
>

**Your Thinking and Examples Below**

I think a tech should be able to adopt a problem that is in the AwaitingTechAssignment state. This adoption would change the problem's status to AssignedToTech and associate the tech's ID with the problem. This ensures that the problem is now being actively worked on and prevents other techs from trying to claim it.

POST http://localhost:1337/tech/problems/{problemId}/assign
Accept: application/json
Authorization: Bearer {JWT}

{
    "techId": "54321"
}

If the adoption works:

200 Ok
Content-Type: application/json

{
    "message": "Problem successfully assigned to tech.",
    "problemId": "67890",
    "techId": "54321",
    "status": "AssignedToTech"
}

If the adoption fails because the problem is already assigned to another tech:

400 Bad Request
Content-Type: application/json

{
    "error": "Problem is already assigned to another tech."
}

I would love to introduce a notification system here. For example, when a tech adopts a problem, the employee who submitted the problem could receive a ping (via email or in-app notification) saying, "Your problem has been assigned to Tech [name]." This would improve communication and transparency between the employee and the tech. However, for simplicity, I’ll focus on the adoption functionality itself.