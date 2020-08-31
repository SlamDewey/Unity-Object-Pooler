# Unity Object Pooler

This is a class that can handle many different gameobject pools simultaneously.

## Usage
#### Creating The Object Pooler

To create an instance of the Object Pooler, simply add the script as a component to a game object in the Unity scene.
_Note: Do not create multiple instances of the Object Pooler, the Class uses a Singleton Implementation_
Upon inspecting the host game object, you should see the following:
![A blank object pooler](/images/1.png)

#### Use Parent Transforms Checkbox
The first variable in the inspector window will be a boolean asking whether the Object Pooler should use Parent Transforms.
If checked, the object pooler will organize object pools underneath gameobjects using transform parents.  This is essentially
just used for organization of the insepctor, and will not affect operation.

#### Poolable Types
A poolable type is actually a class defined at the top of the `ObjectPooler.cs` file.  Here is what the inspector of the class
will look like:
![Creating the first PoolableType](/images/2.png)

###### Name
The Name of the PoolableType does not affect operation, and is simply used to organize the List of Poolable Types.

###### Prefab
The Prefab slot is a reference to the prefab that you wish to pool.  The Object Pooler class will use this prefab definition when
instantiating new pooled objects.

###### Sorting Tag
Each object tracked by the object pooler is organized using GameObject Tags.  Therefore each individual pool of objects will only truly be
individual if they each use their own designated tag.  

###### Max
The value of Max is used to define the initial quantity of objects that the pool should instantiate.  After this quantity is created, the object pool
is not capable of creating new objects, unless `Auto Expand` is True.

###### Auto Expand
When the user requests an object from an Object Pool, it may be the case that the Pooler has no objects left to give.  If Auto Expand is set to True, the 
Object Pooler will instantiate and begin tracking a new instance of the Prefab, regardless of the value of Max.

### An Example Of a properly created Object Pooler may look like:
![An Example of an object pooler](/images/3.png)


### Using The Object Pooler In Your Code
_Note: The Object Pooler class uses static functions and a private static singleton, so you should treat the class as if it was static_
#### Creating Objects
When you need to "create" a new object (i.e. you would like to request an object from the pool) you will call the `Instantiate` function.
An object instantiation can have two distinct options:
###### Option 1: Using the Prefab
When Instantiating, you may pass a reference to a Prefab that the Object Pooler is managing,
```
ObjectPooler.Instantiate(ProjectilePrefab, SpawnTransform.position, SpawnTransform.rotation);
```
###### Option 2: Using A Tag
Alternatively, you can pass in the tag that the Object Pooler is assigning to the GameObject's you want.
```
ObjectPooler.Instantiate("PlayerProjectile", SpawnTransform.position, SpawnTransform.rotation);
```

In both cases, if the Identifier (Prefab or Tag) is not found in the set of PoolableTypes, then the Object Pooler will return `null`.
Therefore you can check the return value of the `Instantiate()` function, to know if it was successful or not.


#### Destroying Objects
Destroying objects is just as simple, you will simply ask the Object Pooler to "destroy" the object for you like so:
```
ObjectPooler.Destroy(this.gameObject);
```


#### Special Usability
You may now ask:
> What if I want to do something to an object when it is "destroyed" or "instantiated"?
Well this is simple as well.  The Object Pooler is Activating and Deactiving objects under the hood by using `GameObject.SetActive()`.  Therefore you can implement the functions:
  - `void OnEnable()`
  - `void OnDisable()`
in any `MonoBehaviour`s that you attach to a pooled object.

An example of this would be my Sample Projectile Class:
```
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



