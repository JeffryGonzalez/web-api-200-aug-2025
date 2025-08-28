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

An action of "Cancel" should be done on a "Resource" of our helpdesk API. Cancelling means modifiying a created document. Thereofre PUT is the best method instead of Delete since we want to keep it on record. 

```http
DELETE http://localhost:1337/employee/problems/3948038  // put means replace. 
Accept: application/json
Authorization: Bear{JWT}
```

// DELETE means i dont want the resource to be there but can have it on db.
My reasons: 
We need to include ID of the document for identifying the item. 
Include json format(since it raises error without the Accept:... line). 
Include authorization to verify who is accessing the record for security. 
I also only include status and reason since this is the only necessary information for making the change.

### Assigning a Problem To A Tech

> We need a way for the techs of the Help Desk to see a list of *all* problems that are awaiting tech assignment. It might be nice if they could filter by either the person that submitted them, or by the software they are having an issue with.

**Your Thinking and Examples Below**

Help desk will have an endpoint as such:
```http
GET http://localhost:1337/employee/problems?softwareid=value1&name=value2
Accept: application/json
Authorization: Bear{JWT}
```
Response example:
{
    "total_matching_problems": 2,
    "parameters": ["name","software title"],
    "Problems": 
    [
        {
            "Id": "54323125",
            "status": "Waiting for assignment",
            "cancelReason": "",  // this is blank if not cancelled.
            "Severity": "High",
            "InVIPList":"True",
            "Requestor name": "Samuel Shiau",
            "Software Title": "VS Code",
            "Assigned tech" ""
            //Here we will list all the fields of the problem since tech need all of the info...
        },
        {
            "Id": "12345354",
            "status": "Cencelled",
            "cancelReason": "Resolved by requestor",  
            "Severity": "Low",
            "InVIPList":"False",
            "Requestor name": "Chris",
            "Software Title": "Chrome"
            "Assigned tech": "Harry"
            ....
        }
    ]
   
    
}


> Once a tech has "located" a problem that is awaiting tech assignment, how could, through the API, that tech "adopt" that. What would happen to that problem?
>

**Your Thinking and Examples Below**
Assume tech is using a UI to access all of the existing problems.
When tech found the problem: 
It means he/she see the problem on the UI(after a successful get request), tech will hit a "Adopt button". It triggers a PUT request(modify) as such:


```http
PUT http://localhost:1337/employee/problems?softwareId=398398938
Accept: application/json
Authorization: Bear{JWT}
{
    "id": 398398938,
    "status": "Assigned",
    "Assigned tech": "John"
}
```
I only include the necessary fields for modifying the document.

User(tech) need to see the feedback that the problem has been successfully adopted through the UI. Therefore the body should include the following information to display on UI.
Response body:
{
    id,
    status,
    assigned tech,
    assignment date/time
}