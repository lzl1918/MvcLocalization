# MvcLocalization
Allow Mvc to render Views/Pages according to the language declared in request url or in HTTP header.

## Usage
1. Configure the default language and supported languages in `Startup`.
2. Let your controller be the subclass of `CultureMatchingController`, and let your page model be the subclass of `CultureMatchingPageModel`.
3. Copy your views or pages, add language name before the extension, and localize the content.

`LocalizationTest` provides samples about the usage detail.

## Matching rule
Take `zh-CN` for example:

First, the program will check if an exact match of the required culture exists. If there is a view named `$View$.zh-CN.cshtml`, this file would be rendered.

Then, if such a match does not exist, the program will check if `$View.zh.cshtml$` exists.

If the match failed again, the program will take the default language and try to match view files following the two steps described above.

If the match using default language failed, the `$View$.cshtml` will be used.