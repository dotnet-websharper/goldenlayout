# Golden Layout extension for WebSharper

This is the documentation for the WebSharper binding of the
[Golden Layout](https://golden-layout.com/) JavaScript library. The docs of the original
library can be found [here](http://golden-layout.com/docs/).

This documentation will go over the usage of the WebSharper extension and the differences
between the original library and the extension's API.

## Usage

### ContentItem config

You can create 5 different types of _ContentItems_: `component`, `react-component`,
`column`, `row` and `stack`. The config object would have different special fields added
depending on the item's type. As this would not work very in well a statically typed
environment, the extension takes a little different approach.

1. You have the **Item** struct with all common properties
2. You have a different config struct for each type with their special properties
3. You combine them with a factory method

The result will be a type called `GeneralItemConfig`. **Note:** the `GeneralItemConfig` is
not intended for you to create the config with. It is used when the original library would
return or pass a `ContentItem` config as a parameter. _This means that in these cases you
have to look out for the value of the config object's_ `Type` _property when deciding if
you can ask for type-specific fields!_

The factory methods are all static methods of the `ItemFactory` type.
Method name|Signature
---|---
`CreateComponent`|`(Component * Item) -> GeneralItemConfig`|
`CreateReactComponent`|`(ReactComponent * Item) -> GeneralItemConfig`|
`CreateStack`|`(Stack * Item) -> GeneralItemConfig`|
`CreateColumn`|`Item -> GeneralItemConfig`|
`CreateRow`|`Item -> GeneralItemConfig`|

The `row` and `column` types don't require any special config fields. The other config objects listed here:

__**Component**__
Parameter|Type|Required
---|---|---
`ComponentName`|`string`|**required**
`ComponentName`|`obj`|optional

__**ReactComponent**__
Parameter|Type|Required
---|---|---
`Component`|`string`|**required**
`Props`|`obj`|optional

__**Stack**__
Parameter|Type|Required
---|---|---
ActiveItemIndex|`int`|optional

#### Example


