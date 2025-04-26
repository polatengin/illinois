param stringParamA string = 'hello'

@sys.description('this is deployTimeSuffix param')
param deployTimeSuffix string = newGuid()

@sys.description('this module a')
module modATest './module_a.bicep' = {
  name: 'modATest'
  params: {
    stringParamA: stringParamA
    stringParamB: 'hello!'
    objParam: {
      a: 'b'
    }
    arrayParam: [
      {
        a: 'b'
      }
      'abc'
    ]
  }
}


@sys.description('this module b')
module modB './module_b.bicep' = {
  name: 'modB'
  params: {
    location: 'West US'
  }
}

@sys.description('this is just module b with a condition')
module modBWithCondition './module_b.bicep' = if (1 + 1 == 2) {
  name: 'modBWithCondition'
  params: {
    location: 'East US'
  }
}

module modC './module_c.json' = {
  name: 'modC'
  params: {
    location: 'West US'
  }
}

module modCWithCondition './module_c.json' = if (2 - 1 == 1) {
  name: 'modCWithCondition'
  params: {
    location: 'East US'
  }
}

module optionalWithNoParams1 './module_e.bicep'= {
  name: 'optionalWithNoParams1'
}

module optionalWithNoParams2 './module_e.bicep'= {
  name: 'optionalWithNoParams2'
  params: {
  }
}

module optionalWithAllParams './module_e.bicep'= {
  name: 'optionalWithNoParams3'
  params: {
    optionalString: 'abc'
    optionalInt: 42
    optionalObj: { }
    optionalArray: [ ]
  }
}

@description('this is a resource with dependencies')
resource resWithDependencies 'Mock.Rp/mockResource@2020-01-01' = {
  name: 'harry'
  properties: {
    modADep: modATest.outputs.stringOutputA
    modBDep: modB.outputs.myResourceId
    modCDep: modC.outputs.myResourceId
  }
}

module optionalWithAllParamsAndManualDependency './module_e.bicep'= {
  name: 'optionalWithAllParamsAndManualDependency'
  params: {
    optionalString: 'abc'
    optionalInt: 42
    optionalObj: { }
    optionalArray: [ ]
  }
  dependsOn: [
    resWithDependencies
    optionalWithAllParams
  ]
}

module optionalWithImplicitDependency './module_e.bicep'= {
  name: 'optionalWithImplicitDependency'
  params: {
    optionalString: concat(resWithDependencies.id, optionalWithAllParamsAndManualDependency.name)
    optionalInt: 42
    optionalObj: { }
    optionalArray: [ ]
  }
}

module moduleWithCalculatedName './module_e.bicep'= {
  name: '${optionalWithAllParamsAndManualDependency.name}${deployTimeSuffix}'
  params: {
    optionalString: concat(resWithDependencies.id, optionalWithAllParamsAndManualDependency.name)
    optionalInt: 42
    optionalObj: { }
    optionalArray: [ ]
  }
}

@description('this is a resource with calculated name')
resource resWithCalculatedNameDependencies 'Mock.Rp/mockResource@2020-01-01' = {
  name: '${optionalWithAllParamsAndManualDependency.name}${deployTimeSuffix}'
  properties: {
    modADep: moduleWithCalculatedName.outputs.outputObj
  }
}

@description('this is a string output')
output stringOutputA string = modATest.outputs.stringOutputA
output stringOutputB string = modATest.outputs.stringOutputB
@description('this is an object output')
output objOutput object = modATest.outputs.objOutput
output arrayOutput array = modATest.outputs.arrayOutput
output modCalculatedNameOutput object = moduleWithCalculatedName.outputs.outputObj

@sys.description('this is myModules')
var myModules = [
  {
    name: 'one'
    location: 'eastus2'
  }
  {
    name: 'two'
    location: 'westus'
  }
]

var emptyArray = []

module storageResources 'module_a.bicep' = [for module in myModules: {
  name: module.name
  params: {
    arrayParam: []
    objParam: module
    stringParamB: module.location
  }
}]

module storageResourcesWithIndex 'module_a.bicep' = [for (module, i) in myModules: {
  name: module.name
  params: {
    arrayParam: [
      i + 1
    ]
    objParam: module
    stringParamB: module.location
    stringParamA: concat('a', i)
  }
}]

module nestedModuleLoop 'module_a.bicep' = [for module in myModules: {
  name: module.name
  params: {
    arrayParam: [for i in range(0,3): concat('test-', i, '-', module.name)]
    objParam: module
    stringParamB: module.location
  }
}]

module duplicateIdentifiersWithinLoop 'module_a.bicep' = [for x in emptyArray:{
  name: 'hello-${x}'
  params: {
    objParam: {}
    stringParamA: 'test'
    stringParamB: 'test'
    arrayParam: [for x in emptyArray: x]
  }
}]

var duplicateAcrossScopes = 'hello'
module duplicateInGlobalAndOneLoop 'module_a.bicep' = [for duplicateAcrossScopes in []: {
  name: 'hello-${duplicateAcrossScopes}'
  params: {
    objParam: {}
    stringParamA: 'test'
    stringParamB: 'test'
    arrayParam: [for x in emptyArray: x]
  }
}]

var someDuplicate = true
var otherDuplicate = false
module duplicatesEverywhere 'module_a.bicep' = [for someDuplicate in []: {
  name: 'hello-${someDuplicate}'
  params: {
    objParam: {}
    stringParamB: 'test'
    arrayParam: [for otherDuplicate in emptyArray: '${someDuplicate}-${otherDuplicate}']
  }
}]

module propertyLoopInsideParameterValue 'module_a.bicep' = {
  name: 'propertyLoopInsideParameterValue'
  params: {
    objParam: {
      a: [for i in range(0,10): i]
      b: [for i in range(1,2): i]
      c: {
        d: [for j in range(2,3): j]
      }
      e: [for k in range(4,4): {
        f: k
      }]
    }
    stringParamB: ''
    arrayParam: [
      {
        e: [for j in range(7,7): j]
      }
    ]
  }
}

module propertyLoopInsideParameterValueWithIndexes 'module_a.bicep' = {
  name: 'propertyLoopInsideParameterValueWithIndexes'
  params: {
    objParam: {
      a: [for (i, i2) in range(0,10): i + i2]
      b: [for (i, i2) in range(1,2): i / i2]
      c: {
        d: [for (j, j2) in range(2,3): j * j2]
      }
      e: [for (k, k2) in range(4,4): {
        f: k
        g: k2
      }]
    }
    stringParamB: ''
    arrayParam: [
      {
        e: [for j in range(7,7): j]
      }
    ]
  }
}

module propertyLoopInsideParameterValueInsideModuleLoop 'module_a.bicep' = [for thing in range(1,2): {
  name: 'propertyLoopInsideParameterValueInsideModuleLoop'
  params: {
    objParam: {
      a: [for i in range(0,10): i + thing]
      b: [for i in range(1,2): i * thing]
      c: {
        d: [for j in range(2,3): j]
      }
      e: [for k in range(4,4): {
        f: k - thing
      }]
    }
    stringParamB: ''
    arrayParam: [
      {
        e: [for j in range(7,7): j % thing]
      }
    ]
  }
}]

resource kv 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
  name: 'testkeyvault'
}

module secureModule1 './module_f.bicep' = {
  name: 'secureModule1'
  params: {
    secureStringParam1: kv.getSecret('mySecret')
    secureStringParam2: kv.getSecret('mySecret','secretVersion')
  }
}

resource scopedKv 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
  name: 'testkeyvault'
  scope: resourceGroup('otherGroup')
}

module secureModule2 './module_f.bicep' = {
  name: 'secureModule2'
  params: {
    secureStringParam1: scopedKv.getSecret('mySecret')
    secureStringParam2: scopedKv.getSecret('mySecret','secretVersion')
  }
}

var vaults = [
  {
    vaultName: 'test-1-kv'
    vaultRG: 'test-1-rg'
    vaultSub: 'abcd-efgh'
  }
  {
    vaultName: 'test-2-kv'
    vaultRG: 'test-2-rg'
    vaultSub: 'ijkl-1adg1'
  }
]
var secrets = [
  {
    name: 'secret01'
    version: 'versionA'
  }
  {
    name: 'secret02'
    version: 'versionB'
  }
]

resource loopedKv 'Microsoft.KeyVault/vaults@2019-09-01' existing = [for vault in vaults: {
  name: vault.vaultName
  scope: resourceGroup(vault.vaultSub, vault.vaultRG)
}]

module secureModuleLooped './module_f.bicep' = [for (secret, i) in secrets: {
  name: 'secureModuleLooped-${i}'
  params: {
    secureStringParam1: loopedKv[i].getSecret(secret.name)
    secureStringParam2: loopedKv[i].getSecret(secret.name, secret.version)
  }
}]

module secureModuleCondition './module_f.bicep' = {
  name: 'secureModuleCondition'
  params: {
    secureStringParam1: true ? kv.getSecret('mySecret') : 'notTrue'
    secureStringParam2: true ? false ? 'false' : kv.getSecret('mySecret','secretVersion') : 'notTrue'
  }
}

module withSpace 'module_d.bicep' = {
  name: 'withSpace'
}

module withSeparateConfig './module_g.bicep' = {
  name: 'withSeparateConfig'
}
