import '@progress/kendo-vue-intl'

export class Guid {
    static readonly empty: any = "00000000-0000-0000-0000-000000000000";

    static equals(a: any, b: any): boolean {
        if (a == null && b == null)
            return true;

        return a.toString().toLowerCase() === b.toString().toLowerCase();
    }
}

export interface UserProfile {
    id: any;
    firstname: string | null;
    lastname: string | null;
    username: string | null;
    email: string | null;
    phonenumber: string | null;
    organization: string | null;
    canAuthorMetric: boolean;
    canSubmitMeasures: boolean;
    isSystemAdministrator: boolean;
}

export interface IMetricListItem {
    id: any;
    author: {
        id: any;
        userName: string;
        firstName: string | null;
        lastName: string | null;
    };
    serviceDeskUrl: string | null;
    title: string;
    status: {
        id: any;
        user: {
            id: any;
            userName: string;
            firstName: string | null;
            lastName: string | null;
        } | null;
        note: string | null;
        metricStatusID: any;
        title: string;
        createdOn: Date;
    };
    domains: [{ id: any; title: string; }] | null;
    frameworkCategories: [{ id: any; title: string; subCategory: string; }] | null;
    resultsType: { id: any, value: string } | null;
}

export interface IMetric {
    id: any;
    timestamp: any;
    authorID: any;
    title: string|null;
    description: string|null;
    justification: string | null;
    expectedResults: string | null;
    createdOn: Date|null;
    modifiedOn: Date|null;
    serviceDeskUrl: string | null;
    author: {
        id: any;
        firstName: string | null;
        lastName: string | null;
        userName: string | null;
    } | null;
    status: {
        id: any;
        createdOn: Date;
        title: string;
        metricStatusID: any;
        note: string | null;
        user: {
            id: any;
            userName: string;
            firstName: string;
            lastName: string;
        }
    } | null;
    resultsTypeID: any;
    domains: any[] | null;
    frameworkCategories: any[] | null;
    bookmarked: boolean | null;
}

export class Metric implements IMetric {

    constructor(data?: IMetric) {
        if (data) {
            this.id = data.id;
            this.authorID = data.authorID;
            this.createdOn = data.createdOn;
            this.description = data.description;
            this.justification = data.justification;
            this.expectedResults = data.expectedResults;
            this.modifiedOn = data.modifiedOn;
            this.serviceDeskUrl = data.serviceDeskUrl;
            this.timestamp = data.timestamp;
            this.title = data.title;
            this.author = data.author;
            this.status = data.status;
            this.resultsTypeID = data.resultsTypeID;
            this.domains = data.domains || [];
            this.frameworkCategories = data.frameworkCategories || [];
            this.bookmarked = data.bookmarked;
        } else {
            this.id = Guid.empty;
            this.authorID = null;
            this.createdOn = null;
            this.description = null;
            this.justification = null;
            this.expectedResults = null;
            this.modifiedOn = null;
            this.serviceDeskUrl = null;
            this.timestamp = null;
            this.title = null;
            this.author = null;
            this.status = null;
            this.resultsTypeID = null;
            this.domains = [];
            this.frameworkCategories = [];
            this.bookmarked = null;
        }
    }

    public id: any;
    public timestamp: any;
    public authorID: any;
    public title: string | null;
    public description: string | null;
    public justification: string | null;
    public expectedResults: string | null;
    public createdOn: Date | null;
    public modifiedOn: Date | null;
    public serviceDeskUrl: string | null;
    public author: any;
    public status: any;
    public resultsTypeID: any;
    public domains: any[] | null;
    public frameworkCategories: any[] | null;
    public bookmarked: boolean | null;

    get createdOnLocal(): Date {
        return kendo.parseDate((this.createdOn || '').toString() + 'Z');
    }

    get modifiedOnLocal(): Date {
        return kendo.parseDate((this.modifiedOn || '').toString() + 'Z');
    }

    get statusText(): string | null {
        return this.status ? this.status.title : null;
    }

    get authorName(): string | null {
        return this.author ? this.author.lastName + ', ' + this.author.firstName : null;
    }

}

export interface IMeasureMetadata {
    id: any;
    submittedByID: any;
    submittedBy: string;
    submittedOn: string;
    suspendedByID: any | null;
    suspendedOn: string | null;
    organization: string | null;
    organizationID: any | null;
    dataSource: string | null;
    dataSourceID: any | null;
    runDate: string;
    dateRangeStart: string;
    dateRangeEnd: string;
    commonDataModel: string | null;
    databaseSystem: string | null;
    network: string | null;
    metricID: any;
    metricTitle: string;
    resultsTypeID: any;
    resultsType: string;
    measureCount: number;
    commonDataModelVersion: string;
    resultsDelimiter: string;
    supportingResources: string | null;
}

export class MeasureMetadata {

    constructor(data?: IMeasureMetadata) {
        if (data == null) {
            this.id = Guid.empty;
            this.submittedByID = Guid.empty;
            this.submittedBy = "";
            this.submittedOn = "";
            this.suspendedByID = null;
            this.suspendedOn = null;
            this.organization = null;
            this.organizationID = null;
            this.dataSource = null;
            this.dataSourceID = null;
            this.runDate = "";
            this.dateRangeStart = "";
            this.dateRangeEnd = "";
            this.commonDataModel = null;
            this.databaseSystem = null;
            this.network = null;
            this.metricID = Guid.empty;
            this.metricTitle = "";
            this.resultsTypeID = Guid.empty;
            this.resultsType = "";
            this.measureCount = 0;
            this.commonDataModelVersion = ""
            this.resultsDelimiter = "";
            this.supportingResources = "";
        } else {
            this.id = data.id;
            this.submittedByID = data.submittedByID;
            this.submittedBy = data.submittedBy;
            this.submittedOn = data.submittedOn;
            this.suspendedByID = data.suspendedByID;
            this.suspendedOn = data.suspendedOn;
            this.organization = data.organization;
            this.organizationID = data.organizationID;
            this.dataSource = data.dataSource;
            this.dataSourceID = data.dataSourceID;
            this.runDate = data.runDate;
            this.dateRangeStart = data.dateRangeStart;
            this.dateRangeEnd = data.dateRangeEnd;
            this.commonDataModel = data.commonDataModel;
            this.databaseSystem = data.databaseSystem;
            this.network = data.network;
            this.metricID = data.metricID;
            this.metricTitle = data.metricTitle;
            this.resultsTypeID = data.resultsTypeID;
            this.resultsType = data.resultsType;
            this.measureCount = data.measureCount;
            this.commonDataModelVersion = data.commonDataModelVersion;
            this.resultsDelimiter = data.resultsDelimiter;
            this.supportingResources = data.supportingResources;
        }
    }

    public id: any;
    public submittedByID: any;
    public submittedBy: string;
    public submittedOn: string;
    public suspendedByID: any | null;
    public suspendedOn: string | null;
    public organization: string | null;
    public organizationID: any | null;
    public dataSource: string | null;
    public dataSourceID: any | null;
    public runDate: string;
    public dateRangeStart: string;
    public dateRangeEnd: string;
    public commonDataModel: string | null;
    public databaseSystem: string | null;
    public network: string | null;
    public metricID: any;
    public metricTitle: string;
    public resultsTypeID: any;
    public resultsType: string;
    public measureCount: number;
    public commonDataModelVersion: string;
    public resultsDelimiter: string;
    public supportingResources: string | null;

    get submittedOnLocal(): Date {
        return kendo.parseDate((this.submittedOn || '').toString() + 'Z');
    }

    get suspendedOnLocal(): Date | null {
        if (this.suspendedOn == null)
            return null;

        return kendo.parseDate(this.suspendedOn.toString() + 'Z');
    }

    /*By appending the Z to the date string kendo will assume that the date is UTC. The measure dates should be timezone agnostic and not specifying the Z will treat as local to the user.*/
    get runDateLocal(): Date {
        return kendo.parseDate((this.runDate || '').toString());
    }

    get dateRangeStartLocal(): Date {
        return kendo.parseDate((this.dateRangeStart || '').toString());
    }

    get dateRangeEndLocal(): Date {
        return kendo.parseDate((this.dateRangeEnd || '').toString());
    }
}

export interface IDomainItem {
    readonly id: any;
    readonly title: string;
}

export interface IResultsTypesItem {
    readonly id: any;
    readonly value: string;
}

export interface IDataQualtityFrameworkCategoryItem {
    readonly id: any;
    readonly title: string;
    readonly subCategory: string;
}

export class DataQualityFrameworkCategoryItem implements IDataQualtityFrameworkCategoryItem {
    readonly id: any;
    readonly title: string;
    readonly subCategory: string;
    readonly displayTitle: string;

    constructor(data: IDataQualtityFrameworkCategoryItem) {
        this.id = data.id;
        this.title = data.title;
        this.subCategory = data.subCategory;

        if (this.subCategory == null || this.subCategory.length == 0) {
            this.displayTitle = this.title;
        } else {
            this.displayTitle = this.title + ' - ' + this.subCategory;
        }

    }
}

export interface IMetricDependenciesResponse {
    readonly domains: IDomainItem[];
    readonly resultsTypes: IResultsTypesItem[];
    readonly frameworkCategories: IDataQualtityFrameworkCategoryItem[];
}

export class MetricDependenciesProvider {
    private readonly _dependencies: IMetricDependenciesResponse;
    readonly frameworkCategories: DataQualityFrameworkCategoryItem[];

    constructor(dependencies: IMetricDependenciesResponse) {
        this._dependencies = dependencies;
        this.frameworkCategories = dependencies.frameworkCategories.map(i => new DataQualityFrameworkCategoryItem(i));
    }

    public get domains(): IDomainItem[] {
        return this._dependencies.domains;
    }

    public get resultsTypes(): IResultsTypesItem[] {
        return this._dependencies.resultsTypes;
    }
}

export enum MetricStatuses {
    Draft = 'af5892ea-807c-4f1d-9989-aa4f00b9cb96',
    Submitted = '91bff71d-6e3b-4d5a-8947-aa4f00b9cb96',
    InReview = 'e7d3591c-d912-42c6-88e2-aa4f00b9cb96',
    Published = '3ce548a3-4e91-4fe0-9d70-aa4f00b9cb96',
    PublishedRequiresAuthentication = 'a56e66ba-6088-49df-9247-aa4f00b9cb96',
    Rejected = '546e8d36-4979-449a-b730-aa4f00b9cb96',
    Inactive = 'ac70e2a2-9c22-4d1e-b378-aa4f00b9cb96',
    Deleted = '0b930582-060a-4fe8-ab50-aa4f00b9cb96'
}

export enum MetricResultsTypes {
    Count = '6b2d1295-f306-4e4b-94df-aa4f00b9cb96',
    Percentage = '84b7d4b6-f939-43ce-9ca1-aa4f00b9cb96',
    Range = '72b755f7-d7fa-4025-927e-aa4f00b9cb96'
}

export interface IDocument {
    removeLink: string;
    nameLink: string;
    id: any;
    name: string;
    size: string;
    mimeType: string;
    createdOn: Date;
    itemID: any
    userName: string;
    firstName: string;
    lastName: string;
}

export class ApiResult<T> {
    readonly errors: string[] | undefined;
    readonly data: T | undefined;

    get hasErrors() {
        return this.errors != undefined && this.errors != null && this.errors.length > 0;
    }
}



