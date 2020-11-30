# VersionModification

对比两个Unity工程指定目录下有没有修改过的文件

## 关键算法

* 求两个列表的交集和并集

* 基于二进制的文件对比

* Unity文件夹和文件的遍历

## 使用方法

* 配置VersionMod.xml，来告诉工具要去检索Assets文件夹下的哪些子文件夹

* 分别选择两个Unity工程的Assets文件作为对比路径

* 点击“保存差异”，将对比出来的结果保存到指定文件夹下

## 应用举例

某些项目需要严格控制版本提交内容，每一次提交版本都需要和上一次做对比，让开发人员知道都有哪些改动，并且要求每一位开发人员要为自己的改动负责，就可以使用这个工具来找出两个版本间的差异
