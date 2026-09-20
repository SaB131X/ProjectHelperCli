module ProjectHelper.Core.ArgTypes

type GitMode = Github | Gitlab | Other of string

type Options = {
    LocalProjectName : string
    GitProjectName   : string          // default val = LocalProjectName
    TemplateName     : string option
    ForceTemplates   : bool
    NixMode          : bool
    GitMode          : string          // default val "github"
    Public           : bool
    Url              : string option
}