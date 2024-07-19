Today I'll be talking about testing and specifically integration testing. I'll go into a bit of theory at the beginning of the presentation and then jump straight into practical examples, since those are the most fun.

You might be lucky and when you're placed in a on-going project as an consultant or a new employee, where there might be already a set of guidelines on how you do testing. If that's you that's great, I'm happy for you. 

I was never as lucky. In every on going project I've joined has a mixture of NUnit- XUnit libraries, Unit tests, a very weird attempts on integration testing that don't test anything else other than which status code the controller is returning, unit tests that depend on each other and sprinkled in some k6 load testing places that didn't make any sense because "just for fun". And usually none of the tests actually tested if the result of the endpoint if correct.

Combination of all of those factors led me to just make changes in the code and avoid making any changes to those tests, since the testing suite was more fragile than the code itself. 

------

There are 2 camps with 2 different definitions of integration testing.
- First camp only calls tests integration tests when NO mocks are used are you're actually calling the real service or real database. 
- Second camp has a bit more flexible definition of integration testing, this is also what Microsoft's documentation tends to agree with, is that integration testing tests the whole picture with mocking kept to minimum. You generally don't use real database for this purpose, you might spin it up in memory or as a Docker container, but you rarely call external services.

When people talk about integration testing vs unit testing they often compare those two as if they're black and white. I've seen this comparison over and over again, and I don't agree with it.
People tend to describe unit tests as 
- Fast
- Isolated
- Repeatable
- Self-validating
- Timely
- "Solitary"

And integration tests as:
- Invokes multiple parts of the system together 
- External dependencies - flaky if you're calling third party system
- Less deterministic. Only goes towards real database / cache and uses real users and authorization.
- "Sociable"

I believe that those definitions can be combined together. 


<!-- ### Integration testing
In the presentation today I'll be broadly covering what defines unit and integration tests, and jump more in depth into example of a methodology that I've created and that I use every time I'm coming into a project and start developing new features. I say new features because it's way more difficult to be adding tests to a system that you don't know what it's doing rather than the one that you're currently working on, and with that it's not easy to convince the client that you spending your precious hours adding all kinds to tests to current working solution is well money spent.  -->

### My testing philosophy

- I only care about the output of the system. 
- I mostly mock out other dependent systems. If you don't do this it might happen that your tests will not pass, if the other system is down. 
	- What I do instead is that I create a Faker and copy paste expected result from the endpoint. Now this approach does not test changes of the other system, but it makes tests pass way quicker and not be flaky.
- I want to be able to test dependency injection. With unit tests you mock out all the dependencies and run tests towards isolated method. I want to test the whole flow all the methods in between me calling the endpoint and returning the result.
- Tests must be short and concise 
- 3 steps:
- // Arrange - can be abstracted away
- // Act - httpClient calling the endpoint
- // Assert - Fluentassertions 

- Grand goal of testing: being able to trust tests in a way where any changes made to the codebase can be pushed out without or with minimal manual testing.


### Enough theory let's get into some code examples.

### Introducing integration testing into new project 


### Mocking authentication and authorization


### Introducing integration testing into ongoing project

- Needs to handle lots of dependencies

### Integration testing and TDD
This is completely my opinion, but I could only do TDD when I only stuck to integration testing. I hope everyone knows what TDD methodology is all about. I must admit I follow it less often then I would like to.

- Red
- Green 
- Refactor
- Red
- Green
- Refactor


https://www.youtube.com/watch?v=pD1mUQr_Z1U