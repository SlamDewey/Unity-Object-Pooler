# Unity Object Pooler
A MonoBehaviour that can handle many different gameobject pools simultaneously.

___
## Table Of Contents
 - [Creating The Object Pooler](https://github.com/SlamDewey/Unity-Object-Pooler#creating-the-object-pooler)
    - [The `UseParentTransforms` Variable](https://github.com/SlamDewey/Unity-Object-Pooler#the-useparenttransforms-variable)
    - [Poolable Types](https://github.com/SlamDewey/Unity-Object-Pooler#poolable-types)
 - [Using The Object Pooler In Your Code](https://github.com/SlamDewey/Unity-Object-Pooler#using-the-object-pooler-in-your-code)
    - [Getting Objects from a Pool](https://github.com/SlamDewey/Unity-Object-Pooler#getting-objects-from-a-pool)
    - [Destroying Objects](https://github.com/SlamDewey/Unity-Object-Pooler#destroying-objects)
    - [Adding Special Functionality](https://github.com/SlamDewey/Unity-Object-Pooler#adding-special-functionality)

___
## Creating The Object Pooler
To create an instance of the Object Pooler, simply add the script as a component to a game object in the Unity scene.
Upon inspecting the host game object, you should see the following:

![A blank object pooler](/images/1.png)

###### _Note: Do not create multiple instances of the Object Pooler, the Class uses Singleton_
___
### The `UseParentTransforms` Variable
The first variable you will see in the inspector is a boolean asking if the Object Pooler should use Parent Transforms.
When checked, the object pooler will organize object pools underneath gameobjects using transform parents.  

This is essentially just used for organization of the insepctor, and will not affect operation.
___
### Poolable Types
The second variable in the inspector is a `List<PoolableType>`. A `PoolableType` is a class defined at the top of the `ObjectPooler.cs`
file, and is used to house variables for the Object Pooler.

![Creating the first PoolableType](/images/2.png)

I will briefly summarize the purpose of each variable below:

 - `Name`
    - The Name of the PoolableType does not affect operation, and is simply used to organize the List of Poolable Types.

 - `Prefab`
    - The Prefab slot stores a reference to the prefab that you want to pool.  The Object Pooler class will use this prefab definition when
      instantiating new objects.

 - `Sorting Tag`
    - Each object tracked by the object pooler is organized using the GameObject's `name` property.
###### _Note: No two `PooledType`'s can use the same `Sorting Tag`_
 - `Max`
    - The value of Max is used to define the initial quantity of objects that the pool should instantiate.

 - `Auto Expand`
    - When the user requests an object from an Object Pool, it may be the case that the Pooler has no objects left to give.  If Auto Expand is set to True, the 
      Object Pooler will instantiate and begin tracking a new instance of the associated Prefab, regardless of the value of `Max`.

#### An example of a properly created `ObjectPooler` may look like:

![An Example of an object pooler](/images/3.png)


___
## Using The Object Pooler In Your Code
###### _Note: The Object Pooler class uses static functions and a private static singleton_

___
### Getting Objects from a Pool

When you need to "create" a new object (i.e. you would like to request an object from the pool) you will call the `Generate` function.
An object instantiation can have two distinct options:

#### Option 1: Using the Prefab
When Instantiating, you may pass a reference to a Prefab that the `ObjectPooler` is managing,
```c#
ObjectPooler.Generate(ProjectilePrefab, SpawnTransform.position, SpawnTransform.rotation);
```
#### Option 2: Using A Tag
Alternatively, you can pass in the sorting tag for a `PoolableType` you've defined:
```c#
ObjectPooler.Generate("PlayerProjectile", SpawnTransform.position, SpawnTransform.rotation);
```

In both cases, if the `Identifier` (Prefab or Tag) is not found in the set of PoolableTypes, then the Object Pooler will return `null`.
Therefore you can _(and should!)_ check the return value of the `Generate()` function, to know if it was successful or not.

___
### Destroying Objects
Destroying objects is just as simple, you will simply ask the Object Pooler to "destroy" the object for you like so:
```c#
ObjectPooler.Destroy(this.gameObject);
```


___
### Adding Special Functionality
You may now wonder:
> What if I want to do something to an object when it is "destroyed" or "instantiated"?

Well this is simple as well.  The Object Pooler is Activating and Deactiving objects under the hood by using `GameObject.SetActive()`.  Therefore you can implement the functions:
  - ```c#
    void OnEnable()
    ```
  - ```c#
    void OnDisable()
    ```
in any `MonoBehaviour` that you attach to a pooled object.

Here is an example Projectile controller for demonstration:
```c#
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StandardProjectile : MonoBehaviour
{
    public float Speed;
    public float TotalAliveTime;
    
    private float timeAlive;

    private void OnDisable()
    {
        timeAlive = 0f;
    }

    private void Update()
    {
        transform.position += transform.right * Speed * Time.deltaTime;
        timeAlive += Time.deltaTime;
        if (timeAlive > TotalAliveTime)
            ObjectPooler.Destroy(gameObject);
    }
}
```

Anybody using this repository should feel free to open an Issue to suggest changes, or fork and create a pull request if you'd like to submit your own changes.

Thanks!
